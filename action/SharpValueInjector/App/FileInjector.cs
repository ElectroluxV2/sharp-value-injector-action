using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SharpValueInjector.App;

public class FileInjector(ILogger<FileInjector> logger, FunctionProcessor functionProcessor)
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
                    var lineNumber = 0;
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync(cancellationToken);
                        if (line is null) break;
                        lineNumber++;

                        // PERF: It should be possible to replace this within a single pass
                        var sb = new StringBuilder(line);
                        foreach (var key in injections)
                        {
                            logger.LogTrace("Trying: {Key}", key);
                            var value = $"{openingToken}{key}{closingToken}";
                            var index = sb.ToString().IndexOf(value, StringComparison.InvariantCulture);
                            if (index != -1)
                            {
                                var replacement = await injectionValueSupplier(key);
                                logger.LogInformation("Key: {Key} was injected with {Value} in {Path}", key, replacement, path);
                                sb.Replace($"{openingToken}{key}{closingToken}", replacement, index, sb.Length - index);
                            }
                        }

                        // TODO: Remove this scope once we may get rid of legacy Octopus like behaviour
                        {
                            // #{(?<ref>[^|\s]+)\s+\|\s+(?<fun>[^}\s]+)}
                            // Edit online at https://regex101.com/r/m0n4hq/3
                            var findFunctionsRegex = new Regex($@"{Regex.Escape(openingToken)}(?<ref>[^|\s]+)\s+\|\s+(?<fun>[^{Regex.Escape(closingToken)}\s]+){Regex.Escape(closingToken)}");
                            var functionMatches = findFunctionsRegex.Matches(line);

                            if (functionMatches.Count > 0)
                            {
                                foreach (Match functionMatch in functionMatches)
                                {
                                    var columnNumber = line.IndexOf(functionMatch.Value, StringComparison.Ordinal) + 1;

                                    logger.LogWarning(
                                        "[DEPRECATION] Line {Line} at {FileName}:{LineNumber}:{ColumnNumber} contains legacy expression ({Expression}), note that all expressions should be defined in variable or secret files instead",
                                        line,
                                        Path.GetFullPath(path),
                                        lineNumber,
                                        columnNumber,
                                        functionMatch.Value
                                            .Replace(functionMatch.Groups["ref"].Value, string.Empty)
                                            .Replace(openingToken, string.Empty)
                                            .Replace(closingToken, string.Empty)
                                            .Trim()
                                    );

                                    var processedValue = await functionProcessor.ProcesAsync("line", functionMatch.Value, openingToken, closingToken);
                                    sb.Replace(functionMatch.Value, processedValue);
                                }
                            }
                        }

                        await writer.WriteLineAsync(sb.ToString());
                    }

                    await writer.FlushAsync(cancellationToken);
                    await Task.Run(() => writer.Flush(), cancellationToken);
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