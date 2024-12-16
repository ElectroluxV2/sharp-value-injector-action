using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using SharpValueInjector.App;
using Spectre.Console;

namespace TestProject1;

public class UnitTest1
{



    private static async Task AssertFileEqualityAsync(string first, string second, CancellationToken cancellationToken = default)
    {
        if (await Task.WhenAll(File.ReadAllTextAsync(first, cancellationToken), File.ReadAllTextAsync(second, cancellationToken)) is not [var beforeLines, var afterLines])
        {
            throw new InvalidOperationException();
        }
        Assert.Equal(beforeLines, afterLines);
    }

    private static string Pp()
    {
        var tempSamples = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try {
            FileSystem.CopyDirectory(Path.Combine(AppContext.BaseDirectory, "Samples"), tempSamples, true);
        } catch (Exception e) {
            AnsiConsole.WriteException(e);
            throw;
        }

        return tempSamples;
    }
    
    
  [Fact]

    public async Task Injection_ShouldWork_WhenInputValueHasSimpleKey()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Simple", "after.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }

    [Fact]
    public async Task Injection_ShouldWork_WhenInputValueHasComplexKey()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information);

        var afterFile = Path.Combine(Samples, "Complex", "after.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }

    [Fact]
    public async Task Injection_ShouldWork_WhenInputValueHasReference()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Reference", "after.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }

    [Fact]
    public async Task Injection_ShouldWork_WhenInputValueHasRecursiveReference()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Recursive", "after.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }

    [Fact]
    public async Task Injection_ShouldWork_WhenInputValueIsNumeric()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Numeric", "after.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }

    [Fact]
    public async Task Injection_ShouldWork_WhenInputValueIsBoolean()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Boolean", "after.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }

    [Fact]
    public async Task Injection_ShouldWork_WhenInputHasConflict()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Conflict", "after.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }


    [Fact]
    public async Task Injection_ShouldWork_WithHierarchicalInputs_Case1()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Hierarchy", "after-c.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }

    [Fact]
    public async Task Injection_ShouldWork_WithHierarchicalInputs_Case2()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var afterFile = Path.Combine(Samples, "Hierarchy", "after-a.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }

    [Fact]
    public async Task Injection_ShouldWork_WithSimplePatternMatching()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var beforeFile = Path.Combine(Samples, "Pattern", "a", "before.yml");
        var afterFile = Path.Combine(Samples, "Pattern", "a", "after.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }

    [Fact]
    public async Task Injection_ShouldWork_WithAdvancedPatternMatching()
    {
        var Samples = Pp();
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
            null!,
            LogLevel.Information
        );

        var beforeFile = Path.Combine(Samples, "Pattern", "before.yml");
        var afterFile = Path.Combine(Samples, "Pattern", "after.yml");

        
        await AssertFileEqualityAsync(beforeFile, afterFile);
    }
}