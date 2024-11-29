namespace SharpValueInjector.App.Injections;

public record PlainTextInjection(string Value) : IInjection
{
    public ValueTask<string> ProvisionInjectionValueAsync() => ValueTask.FromResult(Value);
}