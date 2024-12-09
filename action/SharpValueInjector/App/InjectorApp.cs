using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Shared;
using SharpValueInjector.App.Functions;
using SharpValueInjector.App.Injections;
using Spectre.Console;

namespace SharpValueInjector.App;

public class InjectorApp(
    ILogger<InjectorApp> logger,
    SharpValueInjectionConfiguration configuration,
    FileOrDirectoryWithPatternResolver fileOrDirectoryWithPatternResolver,
    DirectoryWalker directoryWalker,
    HierarchicalInjectionsResolver hierarchicalInjectionsResolver,
    FileInjector fileInjector,
    FileFetcher fileFetcher,
    ConsoleCancellationToken consoleCancellationToken
)
{
    private static readonly ConcurrentDictionary<string, string> ResolvedInjectionValues = new();

    private static async ValueTask<string> GetOrResolveInjectionValue(string key, IInjection injection)
    {
        if (ResolvedInjectionValues.TryGetValue(key, out var cachedValue))
        {
            return cachedValue;
        }

        var resolved = await injection.ProvisionInjectionValueAsync();
        ResolvedInjectionValues[key] = resolved;
        return resolved;
    }

    public static ServiceProvider BuildServiceProvider(string[] outputFiles, string[] variableFiles, string[] secretFiles, bool recurseSubdirectories, bool ignoreCase, string openingToken, string closingToken, string githubActionsPathOption, string githubOutputPath, string[] passthrough, LogLevel logLevel, CancellationToken cancellationToken = default)
    {
        return new ServiceCollection()
            .AddSingleton(new ConsoleCancellationToken(cancellationToken))
            .AddSingleton(new SharpValueInjectionConfiguration(outputFiles, variableFiles, secretFiles, recurseSubdirectories, ignoreCase, openingToken, closingToken, githubActionsPathOption, githubOutputPath, passthrough))
            .AddLogging()
            .AddSerilog(loggerConfiguration =>
            {
                loggerConfiguration.MinimumLevel.Is((LogEventLevel) logLevel);

                loggerConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:l}] {SourceContext}{NewLine}=>{Scope:l} {Message:lj}{NewLine}{Exception}",
                    applyThemeToRedirectedOutput: true,
                    theme: AnsiConsoleTheme.Code
                );

                loggerConfiguration.Enrich.With(new ScopePathSerilogEnricher());
            })
            .AddHttpClient()
            .AddSingleton<InjectorApp>()
            .AddTransient<JsonSlurp>()
            .AddTransient<FileInjector>()
            .AddTransient<HierarchicalInjectionsResolver>()
            .AddTransient<DirectoryWalker>()
            .AddTransient<FileOrDirectoryWithPatternResolver>()
            .AddTransient<UriMapper>()
            .AddTransient<FileFetcher>()
            .AddFunctions()
            .BuildServiceProvider();
    }

    public static async Task<int> BootstrapAsync(string[] outputFiles, string[] variableFiles, string[] secretFiles, bool recurseSubdirectories, bool ignoreCase, string openingToken, string closingToken, string githubActionsPath, string githubOutputPath, string[] passthrough, LogLevel logLevel, CancellationToken cancellationToken = default)
    {
        var serviceProvider = BuildServiceProvider(outputFiles, variableFiles, secretFiles, recurseSubdirectories, ignoreCase, openingToken, closingToken, githubActionsPath, githubOutputPath, passthrough, logLevel, cancellationToken);
        return await serviceProvider.GetRequiredService<InjectorApp>().RunAsync();
    }

    private async Task<int> RunAsync()
    {
        var (variableFilesFromConfiguration, variableDirectoriesAndPatterns, variableFileLinks) = fileOrDirectoryWithPatternResolver.SplitAndValidate(configuration.VariableFiles);
        var (secretFilesFromConfiguration, secretDirectoriesAndPatterns, secretFileLinks) = fileOrDirectoryWithPatternResolver.SplitAndValidate(configuration.SecretFiles);

        var remoteVariableFiles = fileFetcher.FetchFilesAsync(variableFileLinks, consoleCancellationToken);
        var remoteSecretsFiles = fileFetcher.FetchFilesAsync(secretFileLinks, consoleCancellationToken);

        var variableFiles = await directoryWalker
            .WalkAsync(variableDirectoriesAndPatterns, configuration.RecurseSubdirectories, configuration.IgnoreCase)
            .Concat(variableFilesFromConfiguration.ToAsyncEnumerable())
            .Select(File.OpenRead)
            .Concat(remoteVariableFiles)
            .ToListAsync();

        var secretFiles = await directoryWalker
            .WalkAsync(secretDirectoriesAndPatterns, configuration.RecurseSubdirectories, configuration.IgnoreCase)
            .Concat(secretFilesFromConfiguration.ToAsyncEnumerable())
            .Select(File.OpenRead)
            .Concat(remoteSecretsFiles)
            .ToListAsync();

        logger.LogInformation("Variable files count: {VariableFilesCount}", variableFiles.Count);
        logger.LogInformation("Secret files count: {SecretFilesCount}", secretFiles.Count);

        // This will contain all injectable values
        var injections = await hierarchicalInjectionsResolver
            .ResolveAsync(variableFiles, secretFiles, configuration.OpeningToken, configuration.ClosingToken, consoleCancellationToken);

        // Print all resolved injections
        foreach (var (key, injection) in injections)
        {
            logger.LogInformation("Resolved key {Key} with value {LogValue}", key, await injection.ProvisionLogValueAsync(consoleCancellationToken));
        }

        var injectionKeySet = injections.Keys.ToFrozenSet();
        var valueSupplier = new Func<string, ValueTask<string>>(key => GetOrResolveInjectionValue(key, injections[key]));

        var (outputFilesFromConfiguration, outputDirectoriesAndPatterns, _) = fileOrDirectoryWithPatternResolver.SplitAndValidate(configuration.OutputFiles);
        var outputFiles = directoryWalker
            .WalkAsync(outputDirectoriesAndPatterns, configuration.RecurseSubdirectories, configuration.IgnoreCase)
            .Concat(outputFilesFromConfiguration.ToAsyncEnumerable());

        // Concurrent inject
        await outputFiles
            .Select(async path => await fileInjector.InjectAsync(path, configuration.OpeningToken, configuration.ClosingToken, injectionKeySet, valueSupplier, consoleCancellationToken))
            .ToArrayAsync(consoleCancellationToken);

        await HandlePassthroughAsync(injectionKeySet, valueSupplier);

        return 0;
    }

    private async Task HandlePassthroughAsync(FrozenSet<string> injectionKeySet, Func<string, ValueTask<string>> valueSupplier)
    {
        if (configuration.Passthrough.Length == 0)
        {
            logger.LogInformation("No passthrough keys to handle");
            return;
        }

        var console = AnsiConsole.Create(new()
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Interactive = InteractionSupport.No,
        });

        // 1080p full screen
        console.Profile.Width = 200;

        var table = new Table
        {
            Border = TableBorder.Horizontal,
        };

        table.AddColumns(
            new TableColumn("Key")
            {
                NoWrap = true,
            }, new TableColumn("Value")
            {
                NoWrap = true,
            }
        );


        var passthroughOutput = new Dictionary<string, string>();
        foreach (var key in configuration.Passthrough)
        {
            if (!injectionKeySet.Contains(key))
            {
                table.AddRow(new Markup(key), new Markup("No injection found!", new(Color.LightCoral)));
                continue;
            }

            var value = await valueSupplier(key);
            passthroughOutput[key] = value;

            table.AddRow(new Markup(key), new Markup(value, new(Color.Green)));
        }

        var grid = new Grid();
        grid.AddColumn();
        grid.AddRow(new Markup("Passthrough", Color.LightCyan3).Centered());
        grid.AddRow(table);

        console.Write(grid);

        var json = JsonSerializer.Serialize(passthroughOutput, SourceGenerationContext.Default.DictionaryStringString);
        var text = $"resolved={json}";

        await File.WriteAllTextAsync(configuration.GithubOutputPath, text, consoleCancellationToken);
    }
}
