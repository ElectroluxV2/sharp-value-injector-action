// Copyright (C) IHS Markit. All Rights Reserved.
// NOTICE: All information contained herein is, and remains the property of IHS Markit and its suppliers, if any. The intellectual and technical concepts contained herein are proprietary to IHS Markit and its suppliers and may be covered by U.S. and Foreign Patents, patents in process, and are protected by trade secret or copyright law. Dissemination of this information or reproduction of this material is strictly forbidden unless prior written permission is obtained from IHS Markit.

using System.Web;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;

namespace SharpValueInjector.App;

public class UriMapper(ILogger<UriMapper> logger)
{
    public HttpRequestMessage ToHttpRequest(Uri link)
    {
        // log each segment
        logger.LogDebug(
            """
            UserInfo: {UserInfo}
            Host: {Host}
            Port: {Port}
            Scheme: {Scheme}
            Path: {Path}
            Query: {Query}
            Fragment: {Fragment}
            """,
            link.UserInfo,
            link.Host,
            link.Port,
            link.Scheme,
            link.AbsolutePath,
            link.Query,
            Uri.UnescapeDataString(link.Fragment)
        );

        var method = HttpMethod.Get; // TODO: Add support for setting method from fragment
        var uri = new Uri(link.AbsoluteUri[..link.AbsoluteUri.LastIndexOf("#", StringComparison.Ordinal)]);

        var message = new HttpRequestMessage(method, uri);

        var options = HttpUtility.ParseQueryString(link.Fragment[1..]);
        var headersRaw = options["headers"]!.AsSpan().Tokenize(',');
        foreach (var rawHeader in headersRaw)
        {
            var combinedHeader = rawHeader[0] == '\'' && rawHeader[^1] == '\''
                ? rawHeader[1..^1]
                : rawHeader;

            // PERF: Avoiding allocation by using Span
            if (combinedHeader.ToString().Split(":").Select(x => x.Trim()).ToList() is not [var name, var value])
            {
                logger.LogWarning("Header {Header} is not in the correct format", combinedHeader.ToString());
                continue;
            }

            logger.LogDebug("Adding header {Name}: {Value}", name, value);
            message.Headers.Add(name, value);
        }

        return message;
    }
}