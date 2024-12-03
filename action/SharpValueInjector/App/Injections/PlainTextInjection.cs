namespace SharpValueInjector.App.Injections;

public record PlainTextInjection(string Value) : IInjection
{
    public ValueTask<string> ProvisionInjectionValueAsync(CancellationToken cancellationToken) => ValueTask.FromResult(Value);
}