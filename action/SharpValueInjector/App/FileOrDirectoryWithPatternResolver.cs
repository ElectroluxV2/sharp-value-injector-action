using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace SharpValueInjector.App;

public class FileOrDirectoryWithPatternResolver(ILogger<FileOrDirectoryWithPatternResolver> logger)
{
    public (string[] Files, (string Directory, string Pattern)[] DirectoriesWithPattern, string[] Links) SplitAndValidate(string[] filesOrDirectoriesWithPattern)
    {
        var groupedByLinks = filesOrDirectoriesWithPattern
            .GroupBy(x => x.Contains("https://"))
            .ToImmutableDictionary(x => x.Key, x => x.ToArray());

        // Group paths into ones with pattern and paths without pattern
        var groupedByPattern = (groupedByLinks.GetValueOrDefault(false) ?? [])
            .GroupBy(x => x.Contains('*'))
            .ToImmutableDictionary(x => x.Key, x => x.ToArray());

        var files = groupedByPattern.GetValueOrDefault(false) ?? [];
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
            return (Directory: pathWithPattern[..lastSeparatorIndex],
                Pattern: pathWithPattern[(lastSeparatorIndex + 1)..]);
        }).ToArray();
        
        foreach (var (directory, _) in directoriesAndPatterns)
        {
            logger.LogDebug("Validating that directory {Directory} exists", directory);
            
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
            }
        }
        
        return (files, directoriesAndPatterns, groupedByLinks.GetValueOrDefault(true) ?? []);
    }
}
