using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Http;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Tests.TestSupport;

/// <summary>An <see cref="EbayRestClient"/> for the Sandbox wired to a <see cref="FakeHttpMessageHandler"/>.</summary>
internal sealed class EbayClientTestContext
{
    public const string AccessToken = "test-access-token";

    public EbayClientTestContext()
    {
        Options = Microsoft.Extensions.Options.Options.Create(TestSettings.Ebay(EbayEnvironment.Sandbox));
        RestClient = new EbayRestClient(Http, new StaticAccessTokenProvider(), Options);
    }

    public FakeHttpMessageHandler Http { get; } = new();

    public IOptions<EbayOptions> Options { get; }

    public EbayRestClient RestClient { get; }

    private sealed class StaticAccessTokenProvider : IAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken) => Task.FromResult(AccessToken);
    }
}
