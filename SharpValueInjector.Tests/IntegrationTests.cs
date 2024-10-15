using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using SharpValueInjector.App;

namespace SharpValueInjector.Tests;

[NotInParallel] // Dont know how to handle temp directories in parallel
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class IntegrationTests
{
    private static string _samplesDirectory = string.Empty;

    [Before(Test)]
    public Task Setup(TestContext context)
    {
        // Before each test we gonna copy whole Samples directory to temp directory
        _samplesDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        FileSystem.CopyDirectory("Samples", _samplesDirectory, true);

        return Task.CompletedTask;
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasSimpleKey()
    {
        var beforeFile = Path.Combine(_samplesDirectory, "Simple", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(_samplesDirectory, "Simple", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(_samplesDirectory, "Simple", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasComplexKey()
    {
        var beforeFile = Path.Combine(_samplesDirectory, "Complex", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(_samplesDirectory, "Complex", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(_samplesDirectory, "Complex", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasReference()
    {
        var beforeFile = Path.Combine(_samplesDirectory, "Reference", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(_samplesDirectory, "Reference", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(_samplesDirectory, "Reference", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasRecursiveReference()
    {
        var beforeFile = Path.Combine(_samplesDirectory, "Recursive", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(_samplesDirectory, "Recursive", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(_samplesDirectory, "Recursive", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueIsNumeric()
    {
        var beforeFile = Path.Combine(_samplesDirectory, "Numeric", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(_samplesDirectory, "Numeric", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(_samplesDirectory, "Numeric", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueIsBoolean()
    {
        var beforeFile = Path.Combine(_samplesDirectory, "Boolean", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(_samplesDirectory, "Boolean", "input.json")],
            true,
            false,
            "#{",
            "}",
            null,
            LogLevel.Information
        );

        var afterFile = Path.Combine(_samplesDirectory, "Boolean", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(beforeFile).IsTheSame(afterFile);
    }
}