using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using EbaySellerTool.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Core.Ebay.Auth;

public sealed class EbayOAuthClient(IHttpClientFactory httpClientFactory, IOptions<EbayOptions> ebayOptions, TimeProvider timeProvider) : IEbayOAuthClient
{
    public const string HttpClientName = "EbayOAuth";

    private readonly EbayOptions _ebay = ebayOptions.Value;

    private EbayAppCredentials Credentials => _ebay.ActiveCredentials;

    public Uri BuildConsentUrl(string state)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = Credentials.ClientId,
            ["redirect_uri"] = Credentials.RuName,
            ["response_type"] = "code",
            ["scope"] = EbayScopes.ToScopeParameter(EbayScopes.All),
            ["state"] = state,
            ["prompt"] = "login"
        };

        var queryString = string.Join('&', query.Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value)}"));
        return new UriBuilder(EbayEndpoints.AuthorizeUrl(_ebay.Environment)) { Query = queryString }.Uri;
    }

    public async Task<EbayToken> ExchangeAuthorizationCodeAsync(string authorizationCode, CancellationToken cancellationToken)
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authorizationCode,
            ["redirect_uri"] = Credentials.RuName
        }, cancellationToken);

        if (response.RefreshToken is null || response.RefreshTokenExpiresInSeconds is null)
        {
            throw new EbayApiException("eBay did not return a refresh token.");
        }

        var now = timeProvider.GetUtcNow();
        return new EbayToken(
            response.AccessToken,
            now.AddSeconds(response.ExpiresInSeconds),
            response.RefreshToken,
            now.AddSeconds(response.RefreshTokenExpiresInSeconds.Value));
    }

    public async Task<EbayToken> RefreshAsync(EbayToken token, CancellationToken cancellationToken)
    {
        // No scope parameter: eBay then reuses the scopes the user consented to. Sending the current scope list
        // would make refreshes fail for sign-ins made before a scope was added.
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = token.RefreshToken
        }, cancellationToken);

        return token with
        {
            AccessToken = response.AccessToken,
            AccessTokenExpiresAt = timeProvider.GetUtcNow().AddSeconds(response.ExpiresInSeconds)
        };
    }

    private async Task<TokenResponse> RequestTokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, EbayEndpoints.TokenUrl(_ebay.Environment))
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", EncodeClientCredentials());

        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        if (!response.IsSuccessStatusCode || tokenResponse is null || tokenResponse.AccessToken.Length == 0)
        {
            var reason = tokenResponse?.ErrorDescription ?? tokenResponse?.Error ?? response.ReasonPhrase;
            throw new EbayApiException($"eBay sign-in failed ({(int)response.StatusCode}): {reason}");
        }

        return tokenResponse;
    }

    private string EncodeClientCredentials() =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Credentials.ClientId}:{Credentials.ClientSecret}"));
}
