// Because $GITHUB_TOKEN is scoped to repository, we need to support cross repo file fetch by mock composite action
// To do so, we create a new action yaml that will consists of mock composite actions to download files from other repositories
// This program will generate the action yaml

using Shared;

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
    .ReplaceLineEndings(string.Empty)
    .Split(';')
    .Where(x => x.Contains('$'))
    .Select(CompositeActionFetcher.SplitFetchActionLocator);

var yaml  = CompositeActionFetcher.ActionRefsToCompoundCompositeFetchActionYaml(inputFilesWithCompositeAction);

var path = Path.Join(githubWorkspace, "github-action-fetcher", "action.yml");
Directory.CreateDirectory(Path.GetDirectoryName(path)!);

await File.WriteAllLinesAsync(path, yaml);
Console.Out.WriteLine("{0} is generated", path);
