// Copyright (C) IHS Markit. All Rights Reserved.
// NOTICE: All information contained herein is, and remains the property of IHS Markit and its suppliers, if any. The intellectual and technical concepts contained herein are proprietary to IHS Markit and its suppliers and may be covered by U.S. and Foreign Patents, patents in process, and are protected by trade secret or copyright law. Dissemination of this information or reproduction of this material is strictly forbidden unless prior written permission is obtained from IHS Markit.

using SharpValueInjector.App;

namespace SharpValueInjector.Tests;

public class FileOrDirectoryWithPatternResolverTests
{
    [Test]
    [Arguments("/gha/_temp/../_actions", "owner/repo@main$src/sample", "/gha/_actions/owner/repo/main/src/sample")]
    [Arguments("/gha/_actions", "owner/repo@main$src/sample", "/gha/_actions/owner/repo/main/src/sample")]
    public async Task ResolveCompositeActionPath_ShouldReturnCorrectPath_WhenPathIsGiven(string gha, string given, string expcted)
    {
        var result = FileOrDirectoryWithPatternResolver.ResolveCompositeActionPath(gha, given);
        await Assert.That(result).IsEqualTo(expcted);
    }
}