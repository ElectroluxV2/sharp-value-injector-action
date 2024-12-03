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
    private static class AwsSmService
    {
        private static readonly ConcurrentDictionary<string, FrozenDictionary<string, string>> SecretsCache = new();
        private static readonly AmazonSecretsManagerClient Client;

        static AwsSmService()
        {
            // AOT workaround, see https://github.com/aws/aws-sdk-net/issues/3153
            GlobalRuntimeDependencyRegistry.Instance.RegisterSecurityTokenServiceClient(_ => new AmazonSecurityTokenServiceClient(
                new AnonymousAWSCredentials(),
                RegionEndpoint.USEast1
            ));
            Client = new(RegionEndpoint.USEast1);
        }

        public static async ValueTask<string> GetSecretValueAsync(string arnOrId, string keyInsideSecret, CancellationToken cancellationToken)
        {
            return await InternalGetSecretValueAsync(arnOrId, keyInsideSecret, cancellationToken) ?? throw new InvalidDataException($"Aws Secret: `{arnOrId}` does not contain the specified key: '{keyInsideSecret}'");
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Types are present")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "works on my laptop")]
        private static async Task<string?> InternalGetSecretValueAsync(string arnOrId, string keyInsideSecret, CancellationToken cancellationToken)
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
            var value = await Client.GetSecretValueAsync(new()
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

// public static class AwsArnParser
// {
//     public static Range? ExtractRegionFrom(ReadOnlySpan<char> arnOrName)
//     {
//         if (!arnOrName.Contains(':')) return null;
//
//         var enumerator = arnOrName.Split(':'); // 0
//         enumerator.MoveNext(); // 1
//         enumerator.MoveNext(); // 2
//         enumerator.MoveNext(); // 3
//
//         return enumerator.Current;
//     }
// }