using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace SharpValueInjector.App;

public class FileFetcher(ILogger<FileFetcher> logger, UriMapper uriMapper, HttpClient httpClient)
{
    /// <remarks>
    /// Order of files is preserved
    /// </remarks>
    public async IAsyncEnumerable<Stream> FetchFilesAsync(Uri[] files, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tasks = files.Select(async file =>
        {
            var request = uriMapper.ToHttpRequest(file);
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        });

        logger.LogDebug("Sent {Count} requests concurrently", files.Length);

        // Send request concurrently
        var streams= await Task.WhenAll(tasks);

        logger.LogDebug("Received {Count} responses", files.Length);

        // Preserve order of files
        foreach (var stream in streams)
        {
            yield return stream;
        }
    }
}