using SharpValueInjector.App;

namespace SharpValueInjector.Tests;

public class FileOrDirectoryWithPatternResolverTests
{
    [Test]
    [Arguments("/gha/_temp/../_actions", "owner/repo/src/sample@main$inner/path", "/gha/_actions/owner/repo/main/src/sample/inner/path")]
    [Arguments("/gha/_actions", "owner/repo/src/sample@main$inner/path", "/gha/_actions/owner/repo/main/src/sample/inner/path")]
    [Arguments("/gha/_actions", "owner/repo/src/sample@feature/test/version$inner/path", "/gha/_actions/owner/repo/feature/test/version/src/sample/inner/path")]
    public async Task ResolvePathInCompositeAction_ShouldReturnCorrectPath_WhenPathIsGiven(string gha, string given, string expected)
    {
        var result = FileOrDirectoryWithPatternResolver.ResolvePathInCompositeAction(gha, given);
        await Assert.That(result).IsEqualTo(expected);
    }
}