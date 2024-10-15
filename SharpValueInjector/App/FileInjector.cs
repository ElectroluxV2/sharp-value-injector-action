using System.Text;
using Microsoft.Extensions.Logging;

namespace SharpValueInjector.App;

public class FileInjector(ILogger<FileInjector> logger)
{
    public async ValueTask InjectAsync(string path, string openingToken, string closingToken, Dictionary<string, string> injections, CancellationToken cancellationToken)
    {
        logger.LogInformation("Injecting values into file {Path}", path);
        var reader = File.OpenText(path);
        var writer = File.CreateText($"{path}.injected");
         
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;

            // PERF: It should be possible to replace this within a single pass
            var sb = new StringBuilder(line);
            foreach (var (key, value) in injections)
            {
                sb.Replace($"{openingToken}{key}{closingToken}", value.AsSpan());
            }

            await  writer.WriteLineAsync(sb.ToString());
        }

        reader.Close();
        await writer.FlushAsync(cancellationToken);
        writer.Close();
        File.Delete(path);
        File.Move($"{path}.injected", path);
    }
}