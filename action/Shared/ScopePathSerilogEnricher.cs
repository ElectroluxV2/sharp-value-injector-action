using Serilog.Core;
using Serilog.Events;

namespace Shared;

public class ScopePathSerilogEnricher : ILogEventEnricher
{
    /// <summary>
    /// Replaces the "Scope" array property with a period separated scope path string
    /// </summary>
    /// <param name="logEvent"></param>
    /// <param name="propertyFactory"></param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue("Scope", out var sourceContextValue)) return;
        
        var joinedValue = string.Join('.', ExpandOut(sourceContextValue).Select(e => e.ToString("l", null)));
        var enrichProperty = propertyFactory.CreateProperty("ScopePath", joinedValue);
        logEvent.AddOrUpdateProperty(enrichProperty);
    }

    private static IEnumerable<ScalarValue> ExpandOut(LogEventPropertyValue input) => input switch
    {
        SequenceValue sequenceValue => sequenceValue.Elements.SelectMany(ExpandOut),
        ScalarValue scalarValue => Enumerable.Repeat(scalarValue, 1),
        _ => [], // TODO: Handle other types like StructureValue
    };
}