using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace SharpValueInjector.Tests;

public static class FileAssertionsExtensions
{
    public static InvokableValueAssertionBuilder<string> IsTheSame(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return valueSource.RegisterAssertion(
            assertCondition: new FileEqualsExpectedValueAssertCondition(expected),
            argumentExpressions: [doNotPopulateThisValue1]
        );
    }
}

public class FileEqualsExpectedValueAssertCondition(string expected) : EqualsAssertCondition<string>(File.ReadAllText(expected))
{
    protected override string GetFailureMessage() =>
        $"""
        Expected file contents
        BEGIN
        {ExpectedValue}
        END
        Received file contents
        BEGIN
        {(ActualValue is null ? "null" : File.ReadAllText(ActualValue))}
        END
        """;

    protected override bool Passes(string? actualValue, Exception? exception)
    {
        return actualValue is not null && base.Passes(File.ReadAllText(actualValue), exception);
    }
}