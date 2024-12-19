using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpValueInjector.App;
using SharpValueInjector.App.Injections;

namespace SharpValueInjector.Tests;

public class JsonSlurpTests
{
    private static byte[] ToBytes([StringSyntax("json")] string json) => Encoding.UTF8.GetBytes(json);
    
    [Test]
    public async Task FlattenVariables_JsonWithNestedObjects_ReturnsFlattenedDictionary()
    {
        // Arrange
        var serviceProvider = InjectorApp.BuildServiceProvider(default!, default!, default!, default, default, default!, default!, default!, default!, [], LogLevel.Debug);
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
        var result = slurp.FlattenVariables(json);

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result["a"]).IsEqualTo("1");
        await Assert.That(result["b.c"]).IsEqualTo("2");
        await Assert.That(result["b.d.e"]).IsEqualTo("3");
    }

    public static IEnumerable<Func<(string json, string expectedKey, AwsSmInjection expectedInjection)>> FlattenSecretsTestData()
    {
        yield return () => new(
            // language=json
            """
            {
              "a": {
                "type": "aws-sm-dictionary",
                "secretId": "aws-arn",
                "key": "key"
              }
            }
            """,
            "a",
            new("aws-arn", "key")
        );

        yield return () => new(
            // language=json
            """
            {
              "type": {
                "type": "aws-sm-dictionary",
                "secretId": "aws-arn",
                "key": "type"
              }
            }
            """,
            "type",
            new("aws-arn", "type")
        );

        yield return () => new(
            // language=json
            """
            {
              "type": {
                "aa": {
                  "type": "aws-sm-dictionary",
                  "secretId": "aws-arn",
                  "key": "type"
                },
                "type": {
                  "type": {
                    "aws-sm-dictionary": {
                      "type": {
                        "type": "aws-sm-dictionary",
                        "secretId": "aws-arn",
                        "key": "type"
                      }
                    }
                  }
                }
              }
            }
            """,
            "type.type.type.aws-sm-dictionary.type",
            new("aws-arn", "type")
        );

        yield return () => new(
            // language=json
            """
            {
              "a": {
                "type": "aws-sm-dictionary",
                "secretId": "a",
                "key": "a"
              },
              "a": {
                "type": "aws-sm-dictionary",
                "secretId": "b",
                "key": "b"
              }
            }
            """,
            "a",
            new("b", "b")
        );
    }

    [Test]
    [MethodDataSource(nameof(FlattenSecretsTestData))]
    public async Task FlattenSecrets_ShouldWork([StringSyntax("json")] string json, string expectedKey, AwsSmInjection expectedInjection)
    {
        // Arrange
        var serviceProvider = InjectorApp.BuildServiceProvider(default!, default!, default!, default, default, default!, default!, default!, default!, [], LogLevel.Debug);
        var slurp = serviceProvider.GetRequiredService<JsonSlurp>();

        // Act
        var result = slurp.FlattenSecrets(ToBytes(json));

        // Assert
        await Assert.That(result.ContainsKey(expectedKey)).IsTrue();
        await Assert.That(result[expectedKey]).IsEqualTo(expectedInjection);
    }
}
