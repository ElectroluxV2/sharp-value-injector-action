using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SharpValueInjector.App;

public class HierarchicalInjectionsResolver(ILogger<HierarchicalInjectionsResolver> logger, JsonSlurp jsonSlurp)
{
    public async Task<FrozenDictionary<string, string>> MakeFromInputFilesAsync(IReadOnlyCollection<Stream> inputFiles, string openingToken, string closingToken, CancellationToken cancellationToken)
    {
        // This will contain all injectable values (both plain values & AWS SM ARNs)
        var conflictlessInjections = new Dictionary<string, string>();
        foreach (var inputFile in inputFiles)
        {
            logger.LogInformation("Reading input file {InputFile}", inputFile);

            // PERF: Investigate stackalloc byte buffer for remote files
            var memoryStream = new MemoryStream();
            await inputFile.CopyToAsync(memoryStream, cancellationToken);
            
            var flattened = jsonSlurp.Flatten(memoryStream.ToArray());
            
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
        
        // Each injection may consists of different injections
        var findRefsRegex = new Regex($"{Regex.Escape(openingToken)}(?<ref>[^{Regex.Escape(closingToken)}]+){Regex.Escape(closingToken)}");
        var serviceCollection = new ServiceCollection();
        var recursionTracker = new Dictionary<string, bool>();
        foreach (var (key, value) in conflictlessInjections)
        {
            var matches = findRefsRegex.Matches(value);

            if (matches.Count == 0)
            {
                logger.LogInformation("Registering key {Key} => {Value}", key, value);
                serviceCollection.AddKeyedSingleton(key, value);
                continue;
            }
            
            logger.LogInformation("Registering key {Key} => {Value} with dependencies {Dependencies}", key, value, string.Join(',', matches.Select(x => x.Groups["ref"].Value)));
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
                    stringBuilder.Replace($"{openingToken}{refKey}{closingToken}", provider.GetRequiredKeyedService<string>(refKey));
                }
            
                return stringBuilder.ToString();
            });
        }

        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        // Now we can resolve all the injections
        return conflictlessInjections.Keys
            .ToFrozenDictionary(injectionKey => injectionKey, injectionKey => serviceProvider.GetRequiredKeyedService<string>(injectionKey));
    }
}
