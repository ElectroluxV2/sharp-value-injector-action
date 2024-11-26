// Because $GITHUB_TOKEN is scoped to repository, we need to support cross repo file fetch by mock composite action
// To do so, we create a new action yaml that will consists of mock composite actions to download files from other repositories
// This action should output input file list

Console.Out.WriteLine("Generate composite action to fetch files from other repositories");
var inputFiles = Environment.GetEnvironmentVariable("SVI_INPUT");

if (string.IsNullOrEmpty(inputFiles))
{
    Console.Error.WriteLine("SVI_INPUT is not set");
    Environment.Exit(1);
}

Console.Out.WriteLine("Input files: " + inputFiles);
