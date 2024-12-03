using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared;
using SharpValueInjector.App.Injections;

namespace SharpValueInjector.App;

public class JsonSlurp(ILogger<JsonSlurp> logger)
{
    public FrozenDictionary<string, string> FlattenVariables(ReadOnlySpan<byte> json)
    {
        logger.LogDebug("Parsing JSON size: {JsonSize}", Utils.BytesToString(json.Length));

        // PERF: Use stream reader instead of span
        var reader = new Utf8JsonReader(json, new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });
        
        var keyStack = new Stack<string>();
        var lastPropertyName = string.Empty;
        var dictionary = new Dictionary<string, string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                keyStack.Push(lastPropertyName);
            } 
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                keyStack.Pop();
            }
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                lastPropertyName = reader.GetString() ?? string.Empty;
            }
            else if (reader.TokenType is JsonTokenType.String or JsonTokenType.Number or JsonTokenType.Null or JsonTokenType.True or JsonTokenType.False)
            {
                // PERF: We could load some memory blob and reuse it with these string combinations
                var keyFromStack = string.Join('.', keyStack.Reverse().Where(x => x.Trim().Length != 0));
                var lasRedKey = lastPropertyName.Trim();
                
                var key = keyFromStack.Length == 0
                    ? lasRedKey
                    : keyFromStack + "." + lasRedKey;

                var value = reader.ReadCurrentPropertyAsString();
                
                logger.LogDebug("Found key {Key} with value {Value}", key, value);
                if (dictionary.TryAdd(key, value!)) continue;

                logger.LogWarning("Key {Key} already exists, overwriting", key);
                dictionary[key] = value;
            }
        }

        return dictionary.ToFrozenDictionary();
    }

    public FrozenDictionary<string, AwsSmInjection> FlattenSecrets(ReadOnlySpan<byte> json)
    {
        logger.LogDebug("Parsing JSON size: {JsonSize}", Utils.BytesToString(json.Length));

        // PERF: Use stream reader instead of span
        var reader = new Utf8JsonReader(json, new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        var keyStack = new Stack<string>();
        var lastPropertyName = string.Empty;
        var dictionary = new Dictionary<string, AwsSmInjection>();
        var readProperties = new Dictionary<string, string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                keyStack.Push(lastPropertyName);
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (readProperties.TryGetValue("type", out var typeProp))
                {
                    // PERF: We could load some memory blob and reuse it with these string combinations
                    var keyFromStack = string.Join('.', keyStack.Reverse().Where(x => x.Trim().Length != 0));
                    if (typeProp != "aws-sm-dictionary")
                    {
                        throw new NotSupportedException($"{typeProp} is not supported secret type");
                    }

                    if (!readProperties.TryGetValue("secretId", out var secretIdProp))
                    {
                        throw new NotSupportedException($"secretId is required at {keyFromStack}");
                    }

                    if (!readProperties.TryGetValue("key", out var keyProp))
                    {
                        throw new NotSupportedException($"key is required at {keyFromStack}");
                    }

                    var injection = new AwsSmInjection(secretIdProp, keyProp);
                    logger.LogDebug("Found key {Key} with {AwsSmInjection}", keyFromStack, injection);

                    if (!dictionary.TryAdd(keyFromStack, injection))
                    {
                        logger.LogWarning("Key {Key} already exists, overwriting", keyFromStack);
                        dictionary[keyFromStack] = injection;
                    }

                    // Do not leak props for next object
                    readProperties.Clear();
                }

                keyStack.Pop();
            }
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                lastPropertyName = reader.GetString() ?? string.Empty;
            }
            else if (reader.TokenType is JsonTokenType.String or JsonTokenType.Number or JsonTokenType.Null or JsonTokenType.True or JsonTokenType.False)
            {
                var lastReadKey = lastPropertyName.Trim();

                var value = reader.ReadCurrentPropertyAsString();

                readProperties[lastReadKey] = value;
            }
        }

        return dictionary.ToFrozenDictionary();
    }
}

internal static class ReaderExtensions
{
    public static string ReadCurrentPropertyAsString(this Utf8JsonReader reader) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Number => reader.GetDouble().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => string.Empty,
            var token => throw new NotSupportedException("Unsupported token type: " + token),
        };
}
