
using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpValueInjector.Shared;

namespace SharpValueInjector.App;

public class JsonSlurp(ILogger<JsonSlurp> logger)
{
    public FrozenDictionary<string, string> Flatten(ReadOnlySpan<byte> json)
    {
        logger.LogDebug("Parsing JSON size: {JsonSize}", Utils.BytesToString(json.Length));
        
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
                
                var value = reader.TokenType switch
                {
                    JsonTokenType.String => reader.GetString(),
                    JsonTokenType.Number => reader.GetDouble().ToString(CultureInfo.InvariantCulture),
                    JsonTokenType.True => "true",
                    JsonTokenType.False => "false",
                    JsonTokenType.Null => null,
                    _ => throw new NotSupportedException(),
                };
                
                logger.LogDebug("Found key {Key} with value {Value}", key, value);
                if (dictionary.TryAdd(key, value!)) continue;

                logger.LogWarning("Key {Key} already exists, overwriting", key);
                dictionary[key] = value!;
            }
        }

        return dictionary.ToFrozenDictionary();
    }
}