﻿using System.CommandLine;
using Microsoft.Extensions.Logging;
using SharpValueInjector.App;
using Spectre.Console;
using static SharpValueInjector.Shared.ConsoleLifetimeUtils;

var outputFilesArgument = new Argument<string[]>("output", "Files to inject values into or directories to scan (may contain file name patterns such as '/sample/path/*.yaml'.).");
var inputFilesOption = new Option<string[]>("--input", "Path to JSON file that contain values to inject into target files. To specify multiple files, use multiple --input options. Order matters when resolving conflicts.")
{
    IsRequired = true,
};
var recurseSubdirectoriesOption = new Option<bool>("--recurse-subdirectories", () => true, "Weather to scan subdirectories of the given directories.");
var ignoreCaseOption = new Option<bool>("--ignore-case", () => true, "Weather to ignore case when matching file name patterns.");
var openingTokenOption = new Option<string>("--opening-token", () => "#{", "The opening token for variable interpolation.");
var closingTokenOption = new Option<string>("--closing-token", () => "}", "The closing token for variable interpolation.");
var awsSmTokenOption = new Option<string>("--aws-sm-token", "The AWS Secrets Manager token to use for fetching secrets. When not provided, AWS SM secrets detection & fetching is disabled.");
var logLevelOption = new Option<LogLevel>("--log-level",  () => LogLevel.Information, "The minimum level of logs to output.");
var root = new RootCommand("Injects values from given inputs into given files. Supports hierarchical conflict resolution, recursive directory scanning, file name patterns, recursive variable interpolation, secrets fetching, usage statistics.")
{
    inputFilesOption,
    outputFilesArgument,
    recurseSubdirectoriesOption,
    ignoreCaseOption,
    openingTokenOption,
    closingTokenOption,
    awsSmTokenOption,
    logLevelOption,
};

root.SetHandler(async (outputFiles, inputFiles, recurseSubdirectories, ignoreCase, openingToken, closingToken, awsSmToken, logLevel) =>
{
    try
    {
        var cancellationToken = CreateConsoleLifetimeBoundCancellationToken();
        var exitCode = await InjectorApp.BootstrapAsync(outputFiles, inputFiles, recurseSubdirectories, ignoreCase, openingToken, closingToken, awsSmToken, logLevel, cancellationToken);
        Environment.Exit(exitCode);
    }
    catch (TaskCanceledException)
    {
        Environment.Exit(2);
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteException(ex);
        Environment.Exit(1);
    }
}, outputFilesArgument, inputFilesOption, recurseSubdirectoriesOption, ignoreCaseOption, openingTokenOption, closingTokenOption, awsSmTokenOption, logLevelOption);

await root.InvokeAsync(args);
