namespace SharpValueInjector.App;

public record SharpValueInjectionConfiguration(string[] OutputFiles, string[] InputFiles, bool RecurseSubdirectories, bool IgnoreCase, string OpeningToken, string ClosingToken, string? AwsSmToken);