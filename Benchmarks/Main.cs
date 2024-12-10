using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;
using Microsoft.Extensions.Logging;
using SharpValueInjector.App;

namespace Benchmarks;

[SimpleJob(RunStrategy.ColdStart, launchCount: 1, iterationCount: 1)]
public class Main
{
    private static DirectoryInfo _tempDirectory = Directory.CreateTempSubdirectory("sharp-value-injector-benchmark-");

    private async Task GenerateFiles(int cost)
    {
        // E.g.: /var/folders/rl/dp8843dx2v15d0jqlmlhgdsr0000gn/T/sharp-value-injector-benchmark-BYSUKV
        _tempDirectory.Delete(true);
        _tempDirectory.Create();
        var variableDirectory = _tempDirectory.CreateSubdirectory("variable");
        var outputDirectory = _tempDirectory.CreateSubdirectory("output");

        Randomizer.Seed = new(2137);
        var faker = new Faker();

        var outputFileDirectories = Enumerable
            .Range(1, cost)
            .Select(_ => faker.System
                .DirectoryPath()
                .Split(Path.DirectorySeparatorChar)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Aggregate(outputDirectory, (acc, cur) => acc.CreateSubdirectory(cur))
            )
            .ToArray();

        var offTopicFiles = outputFileDirectories
            .Select(path => Path.Combine(path.FullName, faker.System.CommonFileName()));

        foreach (var offTopicFile in offTopicFiles)
        {
            await File.WriteAllTextAsync(offTopicFile, faker.Random.Words(10));
        }

        var outputFiles = outputFileDirectories
            .Select(path => Path.Combine(path.FullName, faker.System.CommonFileName("yml")))
            .Select(File.CreateText)
            .ToArray();

        const string validVariableKeyChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.";
        const string validVariableValueChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";

        for (var fileIndex = 0; fileIndex < 10 * cost; fileIndex++)
        {
            await Console.Out.WriteLineAsync($"Generating file {fileIndex + 1}/{10 * cost}");
            var flatVariables = new Dictionary<string, string>();
            for (var variableIndex = 0; variableIndex < faker.Random.Int(0, 10 * cost); variableIndex++)
            {
                flatVariables[faker.Random.String2(faker.Random.Int(5, 10 * cost), validVariableKeyChars)] = faker.Random.String2(faker.Random.Int(0,  50 * cost), validVariableValueChars);
                await faker.Random.ArrayElement(outputFiles).WriteLineAsync($"file-{fileIndex}-variable-{variableIndex}: #{{{flatVariables.Keys.Last()}}}");
            }

            var variableFilePath = Path.Combine(variableDirectory.FullName, $"variable-{fileIndex}.json");
            var stream = File.Create(variableFilePath);
            await JsonSerializer.SerializeAsync(stream, flatVariables);
        }

        await Console.Out.WriteLineAsync("Path to temp directory: " + _tempDirectory.FullName);
    }

    public IEnumerable<object[]> Arguments()
    {
        yield return [new[] {Path.Combine(_tempDirectory.FullName, "output", "*.yml")}, new[] {Path.Combine(_tempDirectory.FullName, "variable", "*.json")}];
    }

    [GlobalSetup(Target = nameof(Huge))]
    public void SetupHuge()
    {
        GenerateFiles(100).Wait();
    }

    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public async Task Huge(string[] outputFiles, string[] variableFiles)
    {
        _ = await InjectorApp.BootstrapAsync(outputFiles, variableFiles, [], true, true, "#{", "}", string.Empty, string.Empty, [], LogLevel.Information);
    }

    [GlobalSetup(Target = nameof(Average))]
    public void SetupAverage()
    {
        GenerateFiles(5).Wait();
    }

    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public async Task Average(string[] outputFiles, string[] variableFiles)
    {
        _ = await InjectorApp.BootstrapAsync(outputFiles, variableFiles, [], true, true, "#{", "}", string.Empty, string.Empty, [], LogLevel.Information);
    }
}
