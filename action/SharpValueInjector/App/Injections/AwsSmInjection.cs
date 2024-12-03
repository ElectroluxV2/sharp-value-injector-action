using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.RuntimeDependencies;
using Amazon.SecretsManager;
using Amazon.SecurityToken;

namespace SharpValueInjector.App.Injections;

public record AwsSmInjection(string ArnOrId, string KeyInsideSecret) : IInjection
{
    // TODO: Get the fuck out with this rubbish AWS SDK and write a custom typed http client
    private static class AwsSmService
    {
        private static readonly ConcurrentDictionary<string, FrozenDictionary<string, string>> SecretsCache = new();

        private static class AwsSmClientFactory
        {
            // Used for short secret ids
            private static readonly AmazonSecretsManagerClient DefaultRegionClient;
            // Used for full arn secret ids
            private static readonly ConcurrentDictionary<string, AmazonSecretsManagerClient> PerRegionClients = new();

            static AwsSmClientFactory()
            {
                // AOT workaround, see https://github.com/aws/aws-sdk-net/issues/3153
                GlobalRuntimeDependencyRegistry.Instance.RegisterSecurityTokenServiceClient(_ => new AmazonSecurityTokenServiceClient(
                    new AnonymousAWSCredentials()
                ));

                // This will create client in region specified in AWS_REGION, AWS_DEFAULT_REGION or AWS_PROFILE environment variable
                DefaultRegionClient = new(Amazon.Util.EC2InstanceMetadata.Region);
            }

            public static AmazonSecretsManagerClient GetClientForRegion(string? region)
            {
                if (region is null) return DefaultRegionClient;

                if (PerRegionClients.TryGetValue(region, out var client))
                {
                    return client;
                }

                var newClient = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
                PerRegionClients[region] = newClient;
                return newClient;
            }
        }


        public static async ValueTask<string> GetSecretValueAsync(string arnOrId, string keyInsideSecret, CancellationToken cancellationToken)
        {
            return await InternalGetSecretValueAsync(arnOrId, keyInsideSecret, cancellationToken) ?? throw new InvalidDataException($"Aws Secret: `{arnOrId}` does not contain the specified key: '{keyInsideSecret}'");
        }

        private static async ValueTask<string?> InternalGetSecretValueAsync(string arnOrId, string keyInsideSecret, CancellationToken cancellationToken)
        {
            if (SecretsCache.TryGetValue(arnOrId, out var secret))
            {
                return secret.GetValueOrDefault(keyInsideSecret);
            }

            FrozenDictionary<string, string>? dict = null;

            var json = await GetSecretAsync(arnOrId, cancellationToken);
            if (json is not null)
            {
                dict = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.DictionaryStringString)?.ToFrozenDictionary();
            }

            dict ??= FrozenDictionary<string, string>.Empty;

            SecretsCache.TryAdd(arnOrId, dict);
            return dict.GetValueOrDefault(keyInsideSecret);
        }

        private static async ValueTask<string?> GetSecretAsync(string arnOrId, CancellationToken cancellationToken)
        {
            var regionFromArn = arnOrId.Split(':').ElementAtOrDefault(3);
            var client = AwsSmClientFactory.GetClientForRegion(regionFromArn);


            var value = await client.GetSecretValueAsync(new()
            {
                SecretId = arnOrId,
            }, cancellationToken);

            // TODO: Handle other secret types
            return value?.SecretString;
        }
    }

    public ValueTask<string> ProvisionInjectionValueAsync(CancellationToken cancellationToken) => AwsSmService.GetSecretValueAsync(ArnOrId, KeyInsideSecret, cancellationToken);
    public ValueTask<string> ProvisionLogValueAsync() => ValueTask.FromResult(ArnOrId);
}
