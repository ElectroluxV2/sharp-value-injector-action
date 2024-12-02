namespace SharpValueInjector.App.Injections;

public record AwsSmInjection(string ArnOrId, string KeyInsideSecret) : IInjection
{
    private static class AwsSmService
    {
        public static async ValueTask<string> GetSecretValueAsync(string arnOrId, string keyInsideSecret)
        {
            throw new NotImplementedException();
        }
    }

    public ValueTask<string> ProvisionInjectionValueAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<string> ProvisionLogValueAsync() => ValueTask.FromResult(ArnOrId);
}
