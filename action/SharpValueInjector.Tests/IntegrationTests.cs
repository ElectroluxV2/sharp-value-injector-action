using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using SharpValueInjector.App;
using Spectre.Console;

namespace SharpValueInjector.Tests;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
[Retry(0), Timeout(1_000 * 60)]
public class IntegrationTests
{
    [Before(Test)]
    public Task Setup(TestContext context, CancellationToken cancellationToken)
    {
        // Before each test we gonna copy whole Samples directory to temp directory
        var tempSamples = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try {
            FileSystem.CopyDirectory(Path.Combine(AppContext.BaseDirectory, "Samples"), tempSamples, true);
        } catch (Exception e) {
            AnsiConsole.WriteException(e);
            throw;
        }
        
        context.ObjectBag["Samples"] = tempSamples; 

        return Task.CompletedTask;
    }

    #pragma warning disable TUnit0015
    private static string Samples => TestContext.Current!.ObjectBag["Samples"] as string ?? throw new InvalidOperationException();
    #pragma warning restore TUnit0015

    private static async Task AssertFileEqualityAsync(string first, string second, CancellationToken cancellationToken)
    {
        if (await Task.WhenAll(File.ReadAllTextAsync(first, cancellationToken), File.ReadAllTextAsync(second, cancellationToken)) is not [var beforeLines, var afterLines])
        {
            throw new InvalidOperationException();
        }

        await Assert.That(beforeLines).IsEqualTo(afterLines);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasSimpleKey(CancellationToken cancellationToken)
    {
        var beforeFile = Path.Combine(Samples, "Simple", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Simple", "input.json")],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var afterFile = Path.Combine(Samples, "Simple", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasComplexKey(CancellationToken cancellationToken)
    {
        var beforeFile = Path.Combine(Samples, "Complex", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Complex", "input.json")],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var afterFile = Path.Combine(Samples, "Complex", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasReference(CancellationToken cancellationToken)
    {
        var beforeFile = Path.Combine(Samples, "Reference", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Reference", "input.json")],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var afterFile = Path.Combine(Samples, "Reference", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueHasRecursiveReference(CancellationToken cancellationToken)
    {
        var beforeFile = Path.Combine(Samples, "Recursive", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Recursive", "input.json")],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var afterFile = Path.Combine(Samples, "Recursive", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueIsNumeric(CancellationToken cancellationToken)
    {
        var beforeFile = Path.Combine(Samples, "Numeric", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Numeric", "input.json")],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var afterFile = Path.Combine(Samples, "Numeric", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputValueIsBoolean(CancellationToken cancellationToken)
    {
        var beforeFile = Path.Combine(Samples, "Boolean", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Boolean", "input.json")],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var afterFile = Path.Combine(Samples, "Boolean", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Injection_ShouldWork_WhenInputHasConflict(CancellationToken cancellationToken)
    {
        var beforeFile = Path.Combine(Samples, "Conflict", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [Path.Combine(Samples, "Conflict", "input.json")],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var afterFile = Path.Combine(Samples, "Conflict", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }


    [Test]
    public async Task Injection_ShouldWork_WithHierarchicalInputs_Case1(CancellationToken cancellationToken)
    {
        var beforeFile = Path.Combine(Samples, "Hierarchy", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [
                Path.Combine(Samples, "Hierarchy", "input-a.json"),
                Path.Combine(Samples, "Hierarchy", "input-b.json"),
                Path.Combine(Samples, "Hierarchy", "input-c.json"),
            ],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var afterFile = Path.Combine(Samples, "Hierarchy", "after-c.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Injection_ShouldWork_WithHierarchicalInputs_Case2(CancellationToken cancellationToken)
    {
        var beforeFile = Path.Combine(Samples, "Hierarchy", "before.yml");
        var code = await InjectorApp.BootstrapAsync(
            [beforeFile],
            [
                Path.Combine(Samples, "Hierarchy", "input-c.json"),
                Path.Combine(Samples, "Hierarchy", "input-b.json"),
                Path.Combine(Samples, "Hierarchy", "input-a.json"),
            ],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var afterFile = Path.Combine(Samples, "Hierarchy", "after-a.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Injection_ShouldWork_WithSimplePatternMatching(CancellationToken cancellationToken)
    {
        var code = await InjectorApp.BootstrapAsync(
            [Path.Combine(Samples, "Pattern", "a", "*.yml")],
            [Path.Combine(Samples, "Pattern", "a", "*.json")],
            [],
            true,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var beforeFile = Path.Combine(Samples, "Pattern", "a", "before.yml");
        var afterFile = Path.Combine(Samples, "Pattern", "a", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Injection_ShouldWork_WithAdvancedPatternMatching(CancellationToken cancellationToken)
    {
        var code = await InjectorApp.BootstrapAsync(
            [Path.Combine(Samples, "Pattern", "*.yml")],
            [Path.Combine(Samples, "Pattern", "*.json")],
            [],
            false,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Information,
            cancellationToken
        );

        var beforeFile = Path.Combine(Samples, "Pattern", "before.yml");
        var afterFile = Path.Combine(Samples, "Pattern", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }

    [Test]
    public async Task Bas64_ShouldWork(CancellationToken cancellationToken)
    {
        var code = await InjectorApp.BootstrapAsync(
            [Path.Combine(Samples, "Base64", "before.yml")],
            [Path.Combine(Samples, "Base64", "variables.json")],
            [Path.Combine(Samples, "Base64", "secrets.json")],
            false,
            false,
            "#{",
            "}",
            null!,
            null!,
            [],
            LogLevel.Trace,
            cancellationToken
        );

        var beforeFile = Path.Combine(Samples, "Base64", "before.yml");
        var afterFile = Path.Combine(Samples, "Base64", "after.yml");

        await Assert.That(code).IsEqualTo(0);
        await AssertFileEqualityAsync(beforeFile, afterFile, cancellationToken);
    }
}