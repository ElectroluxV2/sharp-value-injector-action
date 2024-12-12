using System.Text;
using Microsoft.Extensions.Logging;

namespace SharpValueInjector.App;

public class FileInjector(ILogger<FileInjector> logger)
{
    public async ValueTask InjectAsync(string path, string openingToken, string closingToken, ISet<string> injections, Func<string, ValueTask<string>> injectionValueSupplier, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Injecting values into file {Path}", path);
            using var _  = logger.BeginScope(Path.GetFileName(path));

            using (var reader = File.OpenText(path))
            {
                await using (var writer = File.CreateText($"{path}.injected"))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync(cancellationToken);
                        if (line is null) break;

                        // PERF: It should be possible to replace this within a single pass
                        var sb = new StringBuilder(line);
                        foreach (var key in injections)
                        {
                            logger.LogTrace("Trying: {Key}", key);
                            sb.Replace($"{openingToken}{key}{closingToken}", await injectionValueSupplier(key));
                        }

                        await writer.WriteLineAsync(sb.ToString());
                    }

                    // reader.Close();
                    // File.Delete(path);
                    //
                    // await writer.FlushAsync(cancellationToken);
                    // writer.Close();
                }
            }
            File.Move($"{path}.injected", path, true);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to inject values into file {Path}", path);
            throw;
        }
    }
}