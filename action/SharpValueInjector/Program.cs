﻿using System.CommandLine;
using Microsoft.Extensions.Logging;
using SharpValueInjector.App;
using Spectre.Console;
using static Shared.ConsoleLifetimeUtils;

var outputFilesArgument = new Argument<string[]>(
    "output",
    () => ArrayFromEnv("SVI_OUTPUT"),
    "Files to inject values into or directories to scan (may contain file name patterns such as '/sample/path/*.yaml'.)."
);

var variableFilesOption = new Option<string[]>(
    "--variable",
    () => ArrayFromEnv("SVI_VARIABLE"),
    "Path to JSON file or directories to scan (may contain file name patterns such as '/sample/path/*.json'.) that contain plain text values to inject into target files. To specify multiple files, use multiple --variable options. Order matters when resolving conflicts."
);

var secretFilesOption = new Option<string[]>(
    "--secret",
    () => ArrayFromEnv("SVI_SECRET"),
    "Path to JSON file or directories to scan (may contain file name patterns such as '/sample/path/*.json'.) that contain references to secrets to inject into target files. To specify multiple files, use multiple --secret options. Order matters when resolving conflicts."
);

var recurseSubdirectoriesOption = new Option<bool>(
    "--recurse-subdirectories",
    () => BoolFromEnv("SVI_RECURSE_SUBDIRECTORIES") ?? true,
    "Weather to scan subdirectories of the given directories."
);

var ignoreCaseOption = new Option<bool>(
    "--ignore-case",
    () => BoolFromEnv("SVI_IGNORE_CASE") ?? true,
    "Weather to ignore case when matching file name patterns."
);

var openingTokenOption = new Option<string>(
    "--opening-token",
    () => Environment.GetEnvironmentVariable("SVI_OPENING") ?? "#{",
    "The opening token for variable interpolation."
);

var closingTokenOption = new Option<string>(
    "--closing-token",
    () => Environment.GetEnvironmentVariable("SVI_CLOSING") ?? "}",
    "The closing token for variable interpolation."
);

var githubActionsPathOption = new Option<string>(
    "--github-actions-path",
    () => Environment.GetEnvironmentVariable("GITHUB_ACTIONS_PATH") ?? string.Empty,
    "For example /gha/_work/_actions, used to resolve composite action references."
);

var githubOutputPathOption = new Option<string>(
    "--github-output-path",
    () => Environment.GetEnvironmentVariable("GITHUB_OUTPUT") ?? string.Empty,
    "For example `/<random guid>/outputs`, used to write GHA output to."
);

var passthroughOption = new Option<string[]>(
    "--passthrough",
    () => ArrayFromEnv("SVI_PASSTHROUGH"),
    "Keys to resolve and pass through to the GitHub Actions output."
);

var logLevelOption = new Option<LogLevel>(
    "--log-level",
    () => Enum.Parse<LogLevel>(Environment.GetEnvironmentVariable("SVI_LOG_LEVEL") ?? "Information"),
    "The minimum level of logs to output."
);

var root = new RootCommand("Injects values from given inputs into given files. Supports hierarchical conflict resolution, recursive directory scanning, file name patterns, recursive variable interpolation, secrets fetching, usage statistics.")
{
    variableFilesOption,
    secretFilesOption,
    outputFilesArgument,
    recurseSubdirectoriesOption,
    ignoreCaseOption,
    openingTokenOption,
    closingTokenOption,
    githubActionsPathOption,
    githubOutputPathOption,
    passthroughOption,
    logLevelOption,
};

root.SetHandler(async context =>
{
    var outputFiles = context.ParseResult.GetValueForArgument(outputFilesArgument);
    var variableFiles = context.ParseResult.GetValueForOption(variableFilesOption)!;
    var secretFiles = context.ParseResult.GetValueForOption(secretFilesOption)!;
    var recurseSubdirectories = context.ParseResult.GetValueForOption(recurseSubdirectoriesOption);
    var ignoreCase = context.ParseResult.GetValueForOption(ignoreCaseOption);
    var openingToken = context.ParseResult.GetValueForOption(openingTokenOption)!;
    var closingToken = context.ParseResult.GetValueForOption(closingTokenOption)!;
    var githubActionsPath = context.ParseResult.GetValueForOption(githubActionsPathOption)!;
    var githubOutputPath = context.ParseResult.GetValueForOption(githubOutputPathOption)!;
    var passthrough = context.ParseResult.GetValueForOption(passthroughOption)!;
    var logLevel = context.ParseResult.GetValueForOption(logLevelOption);

    try
    {
        var cancellationToken = CreateConsoleLifetimeBoundCancellationToken();
        context.ExitCode = await InjectorApp.BootstrapAsync(
            outputFiles,
            variableFiles,
            secretFiles,
            recurseSubdirectories,
            ignoreCase,
            openingToken,
            closingToken,
            githubActionsPath,
            githubOutputPath,
            passthrough,
            logLevel,
            cancellationToken
        );
    }
    catch (TaskCanceledException)
    {
        context.ExitCode = 2;
    }
    catch (Exception ex)
    {
        var console = AnsiConsole.Create(new()
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Interactive = InteractionSupport.No,
        });

        // Do not wrap lines, as github actions log viewer will break them second time
        console.Profile.Width = 2000;

        console.WriteException(ex, new ExceptionSettings
        {
            Format = ExceptionFormats.ShowLinks,
            Style = new()
            {
                // clrs.cc: red
                Message = new(new Color(255, 64, 54), Color.Default, Decoration.Bold),
                // clrs.cc: teal
                Exception = new(new Color(57, 204, 204), Color.Default, Decoration.Bold),
                // clrs.cc: blue
                Method = new(new Color(0, 116, 217), Color.Default, Decoration.Bold),
                // clrs.cc: green
                ParameterType = new Color(46, 204, 64),
                // clrs.cc: orange
                ParameterName = new Color(255, 133, 27),
                // clrs.cc: fuchsia
                Parenthesis = new Color(240, 18, 190),
                // clrs.cc: yellow
                Path = new(new Color(255, 220, 0), Color.Default, Decoration.Bold),
                // clrs.cc: blue
                LineNumber = new Color(0, 116, 217),
                // clrs.cc: silver
                Dimmed = new(new Color(221, 221, 221), Color.Default, Decoration.Italic),
                // clrs.cc: aqua
                NonEmphasized = new Color(127, 219, 255),
            },
        });
        context.ExitCode = 1;
    }
});

return await root.InvokeAsync(args);

bool? BoolFromEnv(string variable)
{
    var value = Environment.GetEnvironmentVariable(variable);
    return value is not null ? bool.Parse(value) : null;
}

string[] ArrayFromEnv(string variable) => Environment.GetEnvironmentVariable(variable)?.ReplaceLineEndings(";").Split(";").Select(x => x.Trim()).ToArray() ?? [];
