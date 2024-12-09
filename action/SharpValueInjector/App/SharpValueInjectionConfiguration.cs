namespace SharpValueInjector.App;

public record SharpValueInjectionConfiguration(
    string[] OutputFiles,
    string[] VariableFiles,
    string[] SecretFiles,
    bool RecurseSubdirectories,
    bool IgnoreCase,
    string OpeningToken,
    string ClosingToken,
    string GithubActionsPath,
    string GithubOutputPath,
    string[] Passthrough
);