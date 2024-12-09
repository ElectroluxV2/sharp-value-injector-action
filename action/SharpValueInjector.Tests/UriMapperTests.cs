using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpValueInjector.App;

namespace SharpValueInjector.Tests;

[Timeout(60 * 1_000)]
public class UriMapperTests
{
    [Test]
    public async Task Parse_ShouldWork_WhenLinkHasFragment(CancellationToken cancellationToken)
    {
        var serviceProvider = InjectorApp.BuildServiceProvider([], [], [], false, false, string.Empty, string.Empty, default!, default!, default!, LogLevel.Debug, cancellationToken);
        var uriConsumer = serviceProvider.GetRequiredService<UriMapper>();

        var request = uriConsumer.ToHttpRequest(new("https://api.github.com/electroluxv2/sharp-value-injector-action/repos/contents/.github/workflows/main.yml?ref=main&ref=v2#headers='Accept: application/vnd.github.raw'&headers='X-GitHub-Api-Version: 2022-11-28'&headers=User-Agent: SharpValueInjector&headers='Authorization: Bearer github_pat_11BitStudios_Broke_My_Heart'"));
        await Assert.That(request.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(request.RequestUri!.AbsoluteUri).IsEqualTo("https://api.github.com/electroluxv2/sharp-value-injector-action/repos/contents/.github/workflows/main.yml?ref=main&ref=v2");
        await Assert.That(request.Headers.GetValues("Accept")).Contains("application/vnd.github.raw");
        await Assert.That(request.Headers.GetValues("X-GitHub-Api-Version")).Contains("2022-11-28");
        await Assert.That(request.Headers.GetValues("Authorization")).Contains("Bearer github_pat_11BitStudios_Broke_My_Heart");
        await Assert.That(request.Headers.GetValues("User-Agent")).Contains("SharpValueInjector");
    }
}