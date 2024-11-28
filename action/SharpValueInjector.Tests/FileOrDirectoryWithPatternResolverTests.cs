// Copyright (C) IHS Markit. All Rights Reserved.
// NOTICE: All information contained herein is, and remains the property of IHS Markit and its suppliers, if any. The intellectual and technical concepts contained herein are proprietary to IHS Markit and its suppliers and may be covered by U.S. and Foreign Patents, patents in process, and are protected by trade secret or copyright law. Dissemination of this information or reproduction of this material is strictly forbidden unless prior written permission is obtained from IHS Markit.

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