using System.IO.Enumeration;
using Microsoft.Extensions.Logging;
using Shared;

namespace SharpValueInjector.App;

public class DirectoryWalker(ILogger<DirectoryWalker> logger, ConsoleCancellationToken cancellationToken)
{
    public async IAsyncEnumerable<string> WalkAsync(IReadOnlyCollection<(string directory, string pattern)> directoriesWithPatterns, bool recurseSubdirectories, bool ignoreCase)
    {
        logger.LogDebug("Got directories to walk: {Directories}, recurse: {Recurse}, ignore case: {IgnoreCase}", directoriesWithPatterns, recurseSubdirectories, ignoreCase);
        
        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true, // We will at least need to read the file contents
            ReturnSpecialDirectories = false, // Ignore .. and .
            RecurseSubdirectories = recurseSubdirectories,
        };
        
        var enumerables = directoriesWithPatterns.Select(tuple => new FileSystemEnumerable<string>(
            tuple.directory,
            (ref FileSystemEntry entry) => entry.ToSpecifiedFullPath(),
            enumerationOptions
        )
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && FileSystemName.MatchesSimpleExpression(tuple.pattern, entry.FileName, ignoreCase),
        });
        
        await foreach (var enumerable in enumerables.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            await foreach (var file in enumerable.ToAsyncEnumerable().WithCancellation(cancellationToken))
            {
                logger.LogInformation("Found file {File}", file);
                yield return file;
            }
        }
    }
}