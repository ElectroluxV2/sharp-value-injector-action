using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace SharpValueInjector.App;

public class FileOrDirectoryWithPatternResolver(ILogger<FileOrDirectoryWithPatternResolver> logger)
{
    public (string[] Files, (string Directory, string Pattern)[] DirectoriesWithPattern) SplitAndValidate(string[] filesOrDirectoriesWithPattern)
    {
        // Group paths into ones with pattern and paths without pattern
        var grouped = filesOrDirectoriesWithPattern
            .GroupBy(x => x.Contains('*'))
            .ToImmutableDictionary(x => x.Key, x => x.ToArray());

        var files = grouped.GetValueOrDefault(false) ?? [];
        var directoriesWithPattern = grouped.GetValueOrDefault(true) ?? [];
        
        // Paths without pattern are supposed to be existing files
        foreach (var file in files)
        {
            logger.LogDebug("Validating that file {File} exists", file);
            
            if (!File.Exists(file))
            {
                throw new FileNotFoundException("File does not exist", file);
            }
        }

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
        
        return (files, directoriesAndPatterns);
    }
}
