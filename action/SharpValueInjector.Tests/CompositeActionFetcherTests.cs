// Copyright (C) IHS Markit. All Rights Reserved.
// NOTICE: All information contained herein is, and remains the property of IHS Markit and its suppliers, if any. The intellectual and technical concepts contained herein are proprietary to IHS Markit and its suppliers and may be covered by U.S. and Foreign Patents, patents in process, and are protected by trade secret or copyright law. Dissemination of this information or reproduction of this material is strictly forbidden unless prior written permission is obtained from IHS Markit.

using SharpValueInjector.App;

namespace SharpValueInjector.Tests;

public class CompositeActionFetcherTests
{

    [Test]
    public async Task SplitShouldWork()
    {
        var (actionRef, filePath) = CompositeActionFetcher.SplitFetchActionLocator("market-intelligence/connect-foundation-mateusz-budzisz/.cicd/compiled@main$variables/connect-foundation-mateusz-budzisz.variables.p1.json");

        await Assert.That(actionRef).IsEqualTo("market-intelligence/connect-foundation-mateusz-budzisz/.cicd/compiled@main");
        await Assert.That(filePath).IsEqualTo("variables/connect-foundation-mateusz-budzisz.variables.p1.json");
    }

    [Test]
    public async Task MapToStepShouldWork()
    {
        var step = CompositeActionFetcher.ActionRefToGitHubStep("market-intelligence/connect-foundation-mateusz-budzisz/.cicd/compiled@main", "variables/connect-foundation-mateusz-budzisz.variables.p1.json");
        var actual = string.Join(Environment.NewLine, step);

        const string expected = // language=yaml
            """
            - name: "Fetch connect-foundation-mateusz-budzisz.variables.p1.json"
              uses: "market-intelligence/connect-foundation-mateusz-budzisz/.cicd/compiled@main"
              if: ${{ '$' }}{{ false }}
            """;

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task ActionRefsToCompoundComppsteFetchActionShouldWork()
    {
        var compositeAction = CompositeActionFetcher.ActionRefsToCompoundCompositeFetchAction(
            ("market-intelligence/connect-foundation-1/.cicd/compiled@main", "variables/connect-foundation-1.variables.p1.json"),
            ("market-intelligence/connect-foundation-2/.cicd/compiled@main", "variables/connect-foundation-2.variables.p2.json")
        );

        var actual = string.Join(Environment.NewLine, compositeAction);

        const string expected = // language=yaml
            """
            name: "Fetch files via composite actions"
            runs:
              using: composite
              steps:
              - name: "Fetch connect-foundation-1.variables.p1.json"
                uses: "market-intelligence/connect-foundation-1/.cicd/compiled@main"
                if: ${{ '$' }}{{ false }}
              - name: "Fetch connect-foundation-2.variables.p2.json"
                uses: "market-intelligence/connect-foundation-2/.cicd/compiled@main"
                if: ${{ '$' }}{{ false }}
            """;

        await Assert.That(actual).IsEqualTo(expected);
    }
}