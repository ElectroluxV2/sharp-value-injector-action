using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace SharpValueInjector.Tests;

public static class FileAssertionsExtensions
{
    public static InvokableValueAssertionBuilder<string> IsTheSame(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return valueSource.RegisterAssertion(
            assertCondition: new FileEqualsExpectedValueAssertCondition(File.ReadAllText(expected)),
            argumentExpressions: [doNotPopulateThisValue1]
        );
    }
}

public class FileEqualsExpectedValueAssertCondition(string expected) : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override string GetExpectation() =>
        $"""
        >>EXPECTED
        {expected}
        <<EXPECTED
        """;

    protected override AssertionResult GetResult(string? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is not null,
                    () => "it was null"
                );
        }

        return AssertionResult.FailIf(
            () => !string.Equals(File.ReadAllText(actualValue), expectedValue),
            () =>
                $"""
                 >>RECEIVED
                 {actualValue}
                 <<RECEIVED
                 """
        );
    }
}