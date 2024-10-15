using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SharpValueInjector.Shared;

namespace SharpValueInjector.App;

public class InjectorApp(ILogger<InjectorApp> logger, SharpValueInjectionConfiguration configuration, IServiceProvider serviceProvider, ConsoleCancellationToken consoleCancellationToken)
{
    public static async Task<int> BootstrapAsync(string[] outputFiles, string[] inputFiles, bool recurseSubdirectories, bool ignoreCase, string openingToken, string closingToken, string? awsSmToken, LogLevel logLevel, CancellationToken cancellationToken = default)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton(new ConsoleCancellationToken(cancellationToken))
            .AddSingleton(new SharpValueInjectionConfiguration(outputFiles, inputFiles, recurseSubdirectories, ignoreCase, openingToken, closingToken, awsSmToken))
            .AddLogging()
            .AddSerilog(loggerConfiguration =>
            {
                loggerConfiguration.MinimumLevel.Is((LogEventLevel) logLevel);

                loggerConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:w3}]{Scope:l} {Message:lj}{NewLine}{Exception}",
                    applyThemeToRedirectedOutput: true,
                    theme: SystemConsoleTheme.Literate
                );
            })
            .AddSingleton<InjectorApp>()
            .AddTransient<JsonSlurp>()
            .AddTransient<FileInjector>()
            .AddTransient<HierarchicalInjectionsResolver>()
            .AddTransient<DirectoryWalker>()
            .AddTransient<FileOrDirectoryWithPatternResolver>()
            .BuildServiceProvider();

        return await serviceProvider.GetRequiredService<InjectorApp>().RunAsync();
    }

    private async Task<int> RunAsync()
    {
        // This will contain all injectable values (both plain values & AWS SM ARNs)
        // TODO: Implement ARN resolution
        var injections = await serviceProvider
            .GetRequiredService<HierarchicalInjectionsResolver>()
            .MakeFromInputFilesAsync(configuration.InputFiles, configuration.OpeningToken, configuration.ClosingToken, consoleCancellationToken);
        
        // Print all resolved injections
        foreach (var (key, value) in injections)
        {
            logger.LogInformation("Resolved key {Key} with value {Value}", key, value);
        }
        
        var fileOrDirectoryWithPatternResolver = serviceProvider.GetRequiredService<FileOrDirectoryWithPatternResolver>();
        var (outputFilesFromConfiguration, outputDirectoriesAndPatterns) = fileOrDirectoryWithPatternResolver.SplitAndValidate(configuration.OutputFiles);
        
        var outputFileWalker = serviceProvider.GetRequiredService<DirectoryWalker>();
        var outputFiles = outputFileWalker
            .WalkAsync(outputDirectoriesAndPatterns, configuration.RecurseSubdirectories, configuration.IgnoreCase)
            .Concat(outputFilesFromConfiguration.ToAsyncEnumerable());
        
        var fileInjector = serviceProvider.GetRequiredService<FileInjector>();

        // Concurrent inject
        await outputFiles
            .Select(async path => await fileInjector.InjectAsync(path, configuration.OpeningToken, configuration.ClosingToken, injections, consoleCancellationToken))
            .ToArrayAsync(consoleCancellationToken);
        
        return 0;
    }
}
