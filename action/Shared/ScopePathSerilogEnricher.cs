// Copyright (C) IHS Markit. All Rights Reserved.
// NOTICE: All information contained herein is, and remains the property of IHS Markit and its suppliers, if any. The intellectual and technical concepts contained herein are proprietary to IHS Markit and its suppliers and may be covered by U.S. and Foreign Patents, patents in process, and are protected by trade secret or copyright law. Dissemination of this information or reproduction of this material is strictly forbidden unless prior written permission is obtained from IHS Markit.

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
        // Console.Out.WriteLine("logEvent.Properties. = {0}", logEvent.Properties["SourceContext"]);
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