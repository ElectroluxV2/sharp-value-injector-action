using System.Diagnostics.CodeAnalysis;
using System.Text;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpValueInjector.App;

namespace SharpValueInjector.Tests;

public class JsonSlurpTests
{
    private static byte[] ToBytes([StringSyntax("json")] string json) => Encoding.UTF8.GetBytes(json);
    
    [Test]
    public async Task Flatten_JsonWithNestedObjects_ReturnsFlattenedDictionary()
    {
        // Arrange
        var serviceProvider = InjectorApp.BuildServiceProvider(default!, default!, default, default, default!, default!, default, LogLevel.Debug);
        var slurp = serviceProvider.GetRequiredService<JsonSlurp>();

        var json = ToBytes(
        """
            {
                "a": 1,
                "b": {
                    "c": 2,
                    "d": {
                        "e": 3
                    }
                }
            }
            """
        );


        // Act
        var result = slurp.Flatten(json);

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result["a"]).IsEqualTo("1");
        await Assert.That(result["b.c"]).IsEqualTo("2");
        await Assert.That(result["b.d.e"]).IsEqualTo("3");
    }

    [Test]
    public async Task SM()
    {
        var client = new AmazonSecretsManagerClient();
        var request = new BatchGetSecretValueRequest
        {
            SecretIdList = [
                "QA/Module/AutoInsight",
                // "arn:aws:secretsmanager:eu-west-1:000209418239:secret:QA/Module/AutoInsight-S75Vg7",
                // "arn:aws:secretsmanager:us-west-2:000209418239:secret:QA/Component/AutoInsight.Data.Service-CLmJj5",
                "arn:aws:secretsmanager:eu-west-1:000209418239:secret:QA/Meta/Connect-hCvfvU",
            ],
        };

        var response = await client.BatchGetSecretValueAsync(request);

        await Assert.That(response).IsNotNull();

        foreach (var value in response.SecretValues)
        {
            await Assert.That(value.SecretString).IsNotNull();
            await Assert.That(value.SecretString.Length).IsGreaterThan(0);
            Console.Out.WriteLine($"SecretString: ({value.Name}) " + value.SecretString);
        }
    }
}