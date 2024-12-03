using System.Text.Json.Serialization;

namespace SharpValueInjector;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class SourceGenerationContext : JsonSerializerContext;
