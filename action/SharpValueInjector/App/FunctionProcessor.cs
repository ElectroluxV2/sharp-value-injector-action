
using Microsoft.Extensions.Logging;
using SharpValueInjector.App.Functions;

namespace SharpValueInjector.App;

public class FunctionProcessor(ILogger<FunctionProcessor> logger, IServiceProvider serviceProvider)
{
    public async Task<string> ProcesAsync(string key, string valueWithFunction, string openingToken, string closingToken)
    {
        using var scope = logger.BeginScope("Key: {Key}", key);

        var indexOfPipe = valueWithFunction.IndexOf('|');
        if (!valueWithFunction.StartsWith(openingToken) || !valueWithFunction.EndsWith(closingToken) || indexOfPipe == -1)
        {
            logger.LogInformation("Does not contain any function, resolved value is {Value}", valueWithFunction);
            return valueWithFunction;
        }

        valueWithFunction = valueWithFunction[openingToken.Length..^closingToken.Length];
        indexOfPipe -= openingToken.Length;

        var valuePart = valueWithFunction[..indexOfPipe].Trim();
        var functionPart = valueWithFunction[(indexOfPipe + 1)..].Trim().ToLowerInvariant();

        logger.LogInformation("Executing {FunctionName} on {Value}", functionPart, valuePart);
        var function = serviceProvider.GetFunction(functionPart);

        if (!await function.TryExecuteAsync(valuePart, out var output))
        {
            throw new InvalidOperationException($"Failed to execute function {functionPart} on value {valuePart} with key {key}");
        }

        logger.LogInformation("Function {FunctionName} resolved to {ResolvedValue}",functionPart, output);
        return output;
    }
}
