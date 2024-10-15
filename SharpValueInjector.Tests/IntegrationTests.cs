using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using SharpValueInjector.App;

namespace SharpValueInjector.Tests;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class IntegrationTests
{
    [Before(Test)]
    public Task Setup(TestContext context)
    {
        // Before each test we gonna copy whole Samples directory to temp directory
        var tempSamples = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        FileSystem.CopyDirectory("Samples", tempSamples, true);
        
        context.ObjectBag["Samples"] = tempSamples; 

        return Task.CompletedTask;
    }
    
    private static string Samples => TestContext.Current!.ObjectBag["Samples"] as string ?? throw new InvalidOperationException();

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasSimpleKey()
    {
        var beforeFile = Path.Combine(Samples, "Simple", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Simple", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Simple", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasComplexKey()
    {
        var beforeFile = Path.Combine(Samples, "Complex", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Complex", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Complex", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasReference()
    {
        var beforeFile = Path.Combine(Samples, "Reference", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Reference", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Reference", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasRecursiveReference()
    {
        var beforeFile = Path.Combine(Samples, "Recursive", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Recursive", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Recursive", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueIsNumeric()
    {
        var beforeFile = Path.Combine(Samples, "Numeric", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Numeric", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Numeric", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueIsBoolean()
    {
        var beforeFile = Path.Combine(Samples, "Boolean", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Boolean", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Boolean", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputHasConflict()
    {
        var beforeFile = Path.Combine(Samples, "Conflict", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Conflict", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Conflict", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }


    [Test]
    public async Task Injection_ShouldWork_WithHierarchicalInputs_Case1()
    {
        var beforeFile = Path.Combine(Samples, "Hierarchy", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [
                Path.Combine(Samples, "Hierarchy", "input-a.json"),
                Path.Combine(Samples, "Hierarchy", "input-b.json"),
                Path.Combine(Samples, "Hierarchy", "input-c.json"),
            ],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Hierarchy", "after-c.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WithHierarchicalInputs_Case2()
    {
        var beforeFile = Path.Combine(Samples, "Hierarchy", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [
                Path.Combine(Samples, "Hierarchy", "input-c.json"),
                Path.Combine(Samples, "Hierarchy", "input-b.json"),
                Path.Combine(Samples, "Hierarchy", "input-a.json"),
            ],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Hierarchy", "after-a.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }
}