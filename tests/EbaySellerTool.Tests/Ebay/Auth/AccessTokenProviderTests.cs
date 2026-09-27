using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace EbaySellerTool.Tests.Ebay.Auth;

public class AccessTokenProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    private readonly InMemoryTokenStore _tokenStore = new();
    private readonly FakeOAuthClient _oauthClient = new();
    private readonly AccessTokenProvider _provider;

    public AccessTokenProviderTests()
    {
        _provider = new AccessTokenProvider(
            _tokenStore, _oauthClient, Options.Create(TestSettings.Ebay(EbayEnvironment.Sandbox)), new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task GetAccessTokenAsync_ValidToken_ReturnsItWithoutRefreshing()
    {
        _tokenStore.Token = new EbayToken("access-1", Now.AddHours(1), "refresh", Now.AddDays(100));

        Assert.Equal("access-1", await _provider.GetAccessTokenAsync(CancellationToken.None));
        Assert.Equal(0, _oauthClient.RefreshCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_TokenAboutToExpire_RefreshesAndSaves()
    {
        _tokenStore.Token = new EbayToken("access-1", Now.AddMinutes(2), "refresh", Now.AddDays(100));

        var accessToken = await _provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.Equal("refreshed-1", accessToken);
        Assert.Equal("refreshed-1", _tokenStore.Token!.AccessToken);
    }

    [Fact]
    public async Task GetAccessTokenAsync_NoStoredToken_ThrowsNotSignedIn()
    {
        await Assert.ThrowsAsync<EbayNotSignedInException>(() => _provider.GetAccessTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefreshTokenExpired_ThrowsNotSignedIn()
    {
        _tokenStore.Token = new EbayToken("access-1", Now.AddHours(-1), "refresh", Now.AddMinutes(-1));

        var exception = await Assert.ThrowsAsync<EbayNotSignedInException>(() => _provider.GetAccessTokenAsync(CancellationToken.None));
        Assert.Contains("expired", exception.Message);
    }

    private sealed class InMemoryTokenStore : ITokenStore
    {
        public EbayToken? Token { get; set; }

        public Task<EbayToken?> LoadAsync(EbayEnvironment environment, CancellationToken cancellationToken) => Task.FromResult(Token);

        public Task SaveAsync(EbayEnvironment environment, EbayToken token, CancellationToken cancellationToken)
        {
            Token = token;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOAuthClient : IEbayOAuthClient
    {
        public int RefreshCount { get; private set; }

        public Uri BuildConsentUrl(string state) => throw new NotSupportedException();

        public Task<EbayToken> ExchangeAuthorizationCodeAsync(string authorizationCode, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EbayToken> RefreshAsync(EbayToken token, CancellationToken cancellationToken)
        {
            RefreshCount++;
            return Task.FromResult(token with { AccessToken = $"refreshed-{RefreshCount}", AccessTokenExpiresAt = Now.AddHours(2) });
        }
    }
}
