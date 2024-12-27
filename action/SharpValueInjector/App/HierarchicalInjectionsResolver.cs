using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpValueInjector.App.Injections;

namespace SharpValueInjector.App;

public class HierarchicalInjectionsResolver(ILogger<HierarchicalInjectionsResolver> logger, JsonSlurp jsonSlurp, FunctionProcessor functionProcessor)
{
    public async Task<FrozenDictionary<string, IInjection>> ResolveAsync(IReadOnlyCollection<Stream> variableFiles, IReadOnlyCollection<Stream> secretFiles, string openingToken, string closingToken, CancellationToken cancellationToken)
    {
        var secretInjections = await ResolveSecretInjectionsAsync(secretFiles, openingToken, closingToken, cancellationToken);
        var variableInjections = await ResolveVariableInjectionsAsync(variableFiles, openingToken, closingToken, cancellationToken);

        return variableInjections.Concat(secretInjections).ToFrozenDictionary();
    }

    private async ValueTask<FrozenDictionary<string, IInjection>> ResolveSecretInjectionsAsync(IReadOnlyCollection<Stream> secretFiles, string openingToken, string closingToken, CancellationToken cancellationToken)
    {
        var conflictlessInjections = new Dictionary<string, IInjection>();
        foreach (var inputFile in secretFiles)
        {
            logger.LogInformation("Reading secret file {InputFile}", inputFile);

            var memoryStream = new MemoryStream();
            await inputFile.CopyToAsync(memoryStream, cancellationToken);

            var flattened = jsonSlurp.FlattenSecrets(memoryStream.ToArray());

            foreach (var (key, injection) in flattened)
            {
                logger.LogInformation("Found key {Key} with injection {Injection}", key, injection);

                if (conflictlessInjections.ContainsKey(key))
                {
                    logger.LogWarning("Key {Key} already exists, overwriting", key);
                }

                conflictlessInjections[key] = injection;
            }
        }

        var findRefsRegex = new Regex($"{Regex.Escape(openingToken)}(?<ref>[^{Regex.Escape(closingToken)}]+){Regex.Escape(closingToken)}");
        var serviceCollection = new ServiceCollection();
        var recursionTracker = new Dictionary<string, bool>();
        foreach (var (key, injection) in conflictlessInjections)
        {
            if (!injection.SupportsExpressions)
            {
                logger.LogTrace("Skipping key {Key} because it does not support expressions", key);
                continue;
            }

            var value = await injection.ProvisionInjectionValueAsync(cancellationToken);
            var processedValue = await functionProcessor.ProcesAsync(key, value, openingToken, closingToken);
            var matches = findRefsRegex.Matches(processedValue);

            if (matches.Count == 0)
            {
                logger.LogInformation("Registering key {Key} => {Value}", key, processedValue);
                serviceCollection.AddKeyedSingleton(key, processedValue);
                continue;
            }

            logger.LogInformation("Registering key {Key} => {Value} with dependencies {Dependencies}", key, processedValue, string.Join(',', matches.Select(x => x.Groups["ref"].Value)));


            serviceCollection.AddKeyedSingleton<string>(key, (provider, _) =>
            {
                if (!recursionTracker.TryAdd(key, true))
                {
                    logger.LogWarning("Recursion detected for key {Key}", key);
                    return $"Error: Recursion detected for key `{key}`!";
                }

                var stringBuilder = new StringBuilder(value);
                foreach (var refKey in matches.Select(x => x.Groups["ref"].Value))
                {

                    var refValue = provider.GetKeyedService<string>(refKey);
                    if (refValue is null)
                    {
                        logger.LogError("Key {Key} has missing dependency: '{DependencyKey}', it will most likely resolve to broken injection", key, refKey);
                        continue;
                    }

                    stringBuilder.Replace($"{openingToken}{refKey}{closingToken}", refValue);
                }

                return stringBuilder.ToString();
            });
        }

        var serviceProvider = serviceCollection.BuildServiceProvider();

        return conflictlessInjections
            .ToFrozenDictionary(
                x => x.Key,
                IInjection (x) => x.Value.SupportsExpressions
                    ? new PlainTextInjection(serviceProvider.GetRequiredKeyedService<string>(x.Key))
                    : x.Value
            );
    }

    private async ValueTask<FrozenDictionary<string, IInjection>> ResolveVariableInjectionsAsync(IReadOnlyCollection<Stream> variableFiles, string openingToken, string closingToken, CancellationToken cancellationToken)
    {
        // This will contain all injectable plain text values
        var conflictlessInjections = new Dictionary<string, string>();
        foreach (var inputFile in variableFiles)
        {
            logger.LogInformation("Reading variable file {InputFile}", inputFile);

            // PERF: Investigate stackalloc byte buffer for remote files
            var memoryStream = new MemoryStream();
            await inputFile.CopyToAsync(memoryStream, cancellationToken);

            var flattened = jsonSlurp.FlattenVariables(memoryStream.ToArray());

            foreach (var (key, value) in flattened)
            {
                logger.LogInformation("Found key {Key} with value {Value}", key, value);

                if (conflictlessInjections.ContainsKey(key))
                {
                    logger.LogWarning("Key {Key} already exists, overwriting", key);
                }

                conflictlessInjections[key] = value;
            }
        }

        // Each plain text injection may consists of different injections
        var findRefsRegex = new Regex($"{Regex.Escape(openingToken)}(?<ref>[^{Regex.Escape(closingToken)}]+){Regex.Escape(closingToken)}");
        var serviceCollection = new ServiceCollection();
        var recursionTracker = new Dictionary<string, bool>();
        foreach (var (key, value) in conflictlessInjections)
        {
            var processedValue = await functionProcessor.ProcesAsync(key, value, openingToken, closingToken);
            var matches = findRefsRegex.Matches(processedValue);

            if (matches.Count == 0)
            {
                logger.LogInformation("Registering key {Key} => {Value}", key, processedValue);
                serviceCollection.AddKeyedSingleton(key, processedValue);
                continue;
            }

            logger.LogInformation("Registering key {Key} => {Value} with dependencies {Dependencies}", key, processedValue, string.Join(',', matches.Select(x => x.Groups["ref"].Value)));


            serviceCollection.AddKeyedSingleton<string>(key, (provider, _) =>
            {
                if (!recursionTracker.TryAdd(key, true))
                {
                    logger.LogWarning("Recursion detected for key {Key}", key);
                    return $"Error: Recursion detected for key `{key}`!";
                }

                var stringBuilder = new StringBuilder(value);
                foreach (var refKey in matches.Select(x => x.Groups["ref"].Value))
                {

                    var refValue = provider.GetKeyedService<string>(refKey);
                    if (refValue is null)
                    {
                        logger.LogError("Key {Key} has missing dependency: '{DependencyKey}', it will most likely resolve to broken injection", key, refKey);
                        continue;
                    }

                    stringBuilder.Replace($"{openingToken}{refKey}{closingToken}", refValue);
                }

                return stringBuilder.ToString();
            });
        }

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Now we can resolve all the injections
        return conflictlessInjections.Keys
            .ToFrozenDictionary<string, string, IInjection>(
                injectionKey => injectionKey,
                injectionKey => new PlainTextInjection(serviceProvider.GetRequiredKeyedService<string>(injectionKey))
            );
    }
}
