using System.Net;
using System.Text;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace EbaySellerTool.Tests.Ebay.Auth;

public class EbayOAuthClientTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeHttpMessageHandler _http = new();
    private readonly EbayOAuthClient _client;

    public EbayOAuthClientTests()
    {
        var options = TestSettings.Ebay(EbayEnvironment.Sandbox);
        options.Sandbox = new EbayAppCredentials { ClientId = "my-app", ClientSecret = "shh", RuName = "My_RuName" };
        _client = new EbayOAuthClient(_http, Options.Create(options), new FakeTimeProvider(Now));
    }

    [Fact]
    public void BuildConsentUrl_TargetsSandboxWithEncodedParameters()
    {
        var url = _client.BuildConsentUrl("abc123");

        Assert.Equal("auth.sandbox.ebay.com", url.Host);
        Assert.Contains("client_id=my-app", url.Query);
        Assert.Contains("redirect_uri=My_RuName", url.Query);
        Assert.Contains("response_type=code", url.Query);
        Assert.Contains("state=abc123", url.Query);
        Assert.Contains("scope=https%3A%2F%2Fapi.ebay.com%2Foauth%2Fapi_scope%20https%3A%2F%2Fapi.ebay.com%2Foauth%2Fapi_scope%2Fsell.inventory", url.Query);
    }

    [Fact]
    public async Task ExchangeAuthorizationCodeAsync_PostsCodeWithBasicAuthAndReturnsToken()
    {
        _http.RespondWith(HttpStatusCode.OK, """
            {"access_token":"access-1","expires_in":7200,"refresh_token":"refresh-1","refresh_token_expires_in":47304000,"token_type":"User Access Token"}
            """);

        var token = await _client.ExchangeAuthorizationCodeAsync("v^1.1#code", CancellationToken.None);

        var (request, body) = Assert.Single(_http.Requests);
        Assert.Equal("https://api.sandbox.ebay.com/identity/v1/oauth2/token", request.RequestUri!.AbsoluteUri);
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal("my-app:shh", Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!)));
        Assert.Equal("grant_type=authorization_code&code=v%5E1.1%23code&redirect_uri=My_RuName", body);

        Assert.Equal(new EbayToken("access-1", Now.AddHours(2), "refresh-1", Now.AddSeconds(47304000)), token);
    }

    [Fact]
    public async Task RefreshAsync_KeepsRefreshTokenAndUpdatesAccessToken()
    {
        _http.RespondWith(HttpStatusCode.OK, """{"access_token":"access-2","expires_in":7200}""");
        var oldToken = new EbayToken("access-1", Now, "refresh-1", Now.AddDays(500));

        var token = await _client.RefreshAsync(oldToken, CancellationToken.None);

        Assert.Equal(new EbayToken("access-2", Now.AddHours(2), "refresh-1", Now.AddDays(500)), token);
        Assert.Equal("grant_type=refresh_token&refresh_token=refresh-1", _http.Requests.Single().Body);
    }

    [Fact]
    public async Task ExchangeAuthorizationCodeAsync_EbayRejectsCode_ThrowsWithEbayReason()
    {
        _http.RespondWith(HttpStatusCode.BadRequest, """{"error":"invalid_grant","error_description":"the provided authorization grant code is invalid"}""");

        var exception = await Assert.ThrowsAsync<EbayApiException>(
            () => _client.ExchangeAuthorizationCodeAsync("expired", CancellationToken.None));

        Assert.Contains("authorization grant code is invalid", exception.Message);
    }
}
