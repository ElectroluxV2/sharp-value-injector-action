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
        var serviceProvider = InjectorApp.BuildServiceProvider(default!, default!, default, default, default!, default!, default!, LogLevel.Debug);
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
}