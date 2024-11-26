// Because $GITHUB_TOKEN is scoped to repository, we need to support cross repo file fetch by mock composite action
// To do so, we create a new action yaml that will consists of mock composite actions to download files from other repositories
// This action should output input file list

using SharpValueInjector.Tests;

Console.Out.WriteLine("Generate composite action to fetch files from other repositories");
var inputFiles = Environment.GetEnvironmentVariable("SVI_INPUT");
var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");

if (string.IsNullOrEmpty(inputFiles))
{
    Console.Error.WriteLine("SVI_INPUT is not set");
    Environment.Exit(1);
}

if (string.IsNullOrEmpty(githubWorkspace))
{
    Console.Error.WriteLine("GITHUB_WORKSPACE is not set");
    Environment.Exit(1);
}

var inputFilesWithCompositeAction = inputFiles
    .Split(';')
    .Where(x => x.Contains('$'))
    .Select(CompositeActionFetcher.SplitFetchActionLocator);

var yaml  = CompositeActionFetcher.ActionRefsToCompoundCompositeFetchActionYaml(inputFilesWithCompositeAction);

await File.WriteAllLinesAsync("fetch-files.yml", yaml);
Console.Out.WriteLine("{0} is generated", Path.GetFullPath("fetch-files.yml"));
