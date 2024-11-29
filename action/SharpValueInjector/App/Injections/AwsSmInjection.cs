namespace SharpValueInjector.App.Injections;

public record AwsSmInjection(string ArnOrId, string KeyInsideSecret) : IInjection
{
    public ValueTask<string> ProvisionInjectionValueAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<string> ProvisionLogValueAsync() => ValueTask.FromResult(ArnOrId);
}
