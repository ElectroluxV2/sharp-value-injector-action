using System.Collections.Concurrent;
using System.Collections.Frozen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Shared;
using SharpValueInjector.App.Functions;
using SharpValueInjector.App.Injections;

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

    public static ServiceProvider BuildServiceProvider(string[] outputFiles, string[] variableFiles, string[] secretFiles, bool recurseSubdirectories, bool ignoreCase, string openingToken, string closingToken, string githubActionsPathOption, LogLevel logLevel, CancellationToken cancellationToken = default)
    {
        return new ServiceCollection()
            .AddSingleton(new ConsoleCancellationToken(cancellationToken))
            .AddSingleton(new SharpValueInjectionConfiguration(outputFiles, variableFiles, secretFiles, recurseSubdirectories, ignoreCase, openingToken, closingToken, githubActionsPathOption))
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

    public static async Task<int> BootstrapAsync(string[] outputFiles, string[] variableFiles, string[] secretFiles, bool recurseSubdirectories, bool ignoreCase, string openingToken, string closingToken, string githubActionsPath, LogLevel logLevel, CancellationToken cancellationToken = default)
    {
        var serviceProvider = BuildServiceProvider(outputFiles, variableFiles, secretFiles, recurseSubdirectories, ignoreCase, openingToken, closingToken, githubActionsPath, logLevel, cancellationToken);
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
        
        return 0;
    }
}
