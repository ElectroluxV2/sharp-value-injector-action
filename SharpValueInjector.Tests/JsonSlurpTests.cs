using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SharpValueInjector.Tests;

public class JsonSlurpTests
{
    private static byte[] ToBytes([StringSyntax("json")] string json) => Encoding.UTF8.GetBytes(json);
    
    [Test]
    public async Task Flatten_JsonWithNestedObjects_ReturnsFlattenedDictionary()
    {
        // Arrange
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
            """);
        
        
        // Act
        // var result = JsonSlurp.Flatten(json);
        
        // Assert
        // await Assert.That(result.Count).IsEqualTo(3);
        // await Assert.That(result["a"]).IsEqualTo("1");
        // await Assert.That(result["b.c"]).IsEqualTo("2");
        // await Assert.That(result["b.d.e"]).IsEqualTo("3");
    }
    
}