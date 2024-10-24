using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SharpValueInjector.Shared;

namespace SharpValueInjector.App;

public class InjectorApp(
    ILogger<InjectorApp> logger,
    SharpValueInjectionConfiguration configuration,
    FileOrDirectoryWithPatternResolver fileOrDirectoryWithPatternResolver,
    DirectoryWalker directoryWalker,
    HierarchicalInjectionsResolver hierarchicalInjectionsResolver,
    FileInjector fileInjector,
    ConsoleCancellationToken consoleCancellationToken
)
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
        var (inputFilesFromConfiguration, inputDirectoriesAndPatterns, inputFileLinks) = fileOrDirectoryWithPatternResolver.SplitAndValidate(configuration.InputFiles);

        logger.LogCritical("Links: {InputLinks}", inputFileLinks);

        var inputFiles = await directoryWalker
            .WalkAsync(inputDirectoriesAndPatterns, configuration.RecurseSubdirectories, configuration.IgnoreCase)
            .Concat(inputFilesFromConfiguration.ToAsyncEnumerable())
            .ToListAsync();

        logger.LogInformation("Input files: {InputFiles}", inputFiles);

        // This will contain all injectable values (both plain values & AWS SM ARNs)
        // TODO: Implement ARN resolution
        var injections = await hierarchicalInjectionsResolver
            .MakeFromInputFilesAsync(inputFiles, configuration.OpeningToken, configuration.ClosingToken, consoleCancellationToken);
        
        // Print all resolved injections
        foreach (var (key, value) in injections)
        {
            logger.LogInformation("Resolved key {Key} with value {Value}", key, value);
        }

        var (outputFilesFromConfiguration, outputDirectoriesAndPatterns, _) = fileOrDirectoryWithPatternResolver.SplitAndValidate(configuration.OutputFiles);
        var outputFiles = directoryWalker
            .WalkAsync(outputDirectoriesAndPatterns, configuration.RecurseSubdirectories, configuration.IgnoreCase)
            .Concat(outputFilesFromConfiguration.ToAsyncEnumerable());

        // Concurrent inject
        await outputFiles
            .Select(async path => await fileInjector.InjectAsync(path, configuration.OpeningToken, configuration.ClosingToken, injections, consoleCancellationToken))
            .ToArrayAsync(consoleCancellationToken);
        
        return 0;
    }
}
