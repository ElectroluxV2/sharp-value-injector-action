using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Shared;

namespace SharpValueInjector.App;

public class FileOrDirectoryWithPatternResolver(ILogger<FileOrDirectoryWithPatternResolver> logger)
{
    public (string[] Files, (string Directory, string Pattern)[] DirectoriesWithPattern, Uri[] Links) SplitAndValidate(string[] filesOrDirectoriesWithPattern)
    {
        var groupedByLinks = filesOrDirectoriesWithPattern
            .GroupBy(x => x.Contains("https://"))
            .ToImmutableDictionary(x => x.Key, x => x.ToArray());

        // Validate that links are correct Urls
        var links = (groupedByLinks.GetValueOrDefault(true) ?? [])
            .Select(link =>
            {
                logger.LogDebug("Validating that link {Link} is a correct URL", link);

                if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
                {
                    throw new ArgumentException($"Link is not a correct URL: {link}");
                }

                return uri;
            })
            .ToArray();

        // Group paths into ones with pattern and paths without pattern
        var groupedByPattern = (groupedByLinks.GetValueOrDefault(false) ?? [])
            .GroupBy(x => x.Contains('*'))
            .ToImmutableDictionary(x => x.Key, x => x.ToArray());

        var files = groupedByPattern.GetValueOrDefault(false) ?? [];

        files = files.Select(x =>
        {
            if (!x.Contains('$'))
                return x;

            var (actionRef, filePath) = CompositeActionFetcher.SplitFetchActionLocator(x);

            var p = actionRef.Split("/");
            var b = actionRef.Substring(actionRef.IndexOf('@') + 1, actionRef.IndexOf('$'));

            for (var i = 2; i < p.Length; i++)
            {
                b += p[i];
            }

            return Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "_actions", p[0], p[1], b, filePath);
        }).ToArray();

        // Paths without pattern are supposed to be existing files
        foreach (var file in files)
        {
            logger.LogDebug("Validating that file {File} exists", file);

            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"File does not exist: {file}", file);
            }
        }

        var directoriesWithPattern = groupedByPattern.GetValueOrDefault(true) ?? [];
        var directoriesAndPatterns = directoriesWithPattern.Select(pathWithPattern =>
        {
            var lastSeparatorIndex = pathWithPattern.LastIndexOf(Path.DirectorySeparatorChar);
            return (Directory: pathWithPattern[..lastSeparatorIndex], Pattern: pathWithPattern[(lastSeparatorIndex + 1)..]);
        }).ToArray();
        
        foreach (var (directory, _) in directoriesAndPatterns)
        {
            logger.LogDebug("Validating that directory {Directory} exists", directory);
            
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
            }
        }
        
        return (files, directoriesAndPatterns, links);
    }
}
