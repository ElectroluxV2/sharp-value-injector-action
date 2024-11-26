// Copyright (C) IHS Markit. All Rights Reserved.
// NOTICE: All information contained herein is, and remains the property of IHS Markit and its suppliers, if any. The intellectual and technical concepts contained herein are proprietary to IHS Markit and its suppliers and may be covered by U.S. and Foreign Patents, patents in process, and are protected by trade secret or copyright law. Dissemination of this information or reproduction of this material is strictly forbidden unless prior written permission is obtained from IHS Markit.

namespace SharpValueInjector.Tests;

public class CompositeActionFetcher
{
    public static (string ActionRef, string filePath) SplitFetchActionLocator(string fetchActionLocator)
    {
        if (fetchActionLocator.Split('$') is not [var first, var second])
        {
            throw new ArgumentException("Invalid fetch action locator", nameof(fetchActionLocator));
        }

        return (first, second);
    }

    public static IEnumerable<string> ActionRefToGitHubStep(string actionRef, string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        // language=yaml
        yield return
            $"""
            - name: "Fetch {fileName}"
            """;

        // language=yaml
        yield return
            $"""
              uses: "{actionRef}"
            """;

        // language=yaml
        yield return
            """
              if: ${{ '$' }}{{ false }}
            """;
    }

    public static IEnumerable<string> ActionRefsToCompoundCompositeFetchActionYaml(params IEnumerable<(string ActionRef, string filePath)> fetchActions)
    {
        // language=yaml
        yield return
            """
            name: "Fetch files via composite actions"
            runs:
              using: composite
              steps:
            """;

        foreach (var (actionRef, filePath) in fetchActions)
        {
            var step = ActionRefToGitHubStep(actionRef, filePath);
            foreach (var line in step)
            {
                yield return "  " + line;
            }
        }
    }
}