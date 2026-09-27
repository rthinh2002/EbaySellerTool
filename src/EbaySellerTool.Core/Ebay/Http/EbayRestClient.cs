using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay.Auth;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Core.Ebay.Http;

/// <summary>Sends signed-in requests to eBay's REST APIs and reads their JSON responses and errors.</summary>
public sealed class EbayRestClient(
    IHttpClientFactory httpClientFactory,
    IAccessTokenProvider accessTokenProvider,
    IOptions<EbayOptions> ebayOptions)
{
    public const string HttpClientName = "EbayApi";
    private const string JsonMediaType = "application/json";
    private const string MarketplaceHeader = "X-EBAY-C-MARKETPLACE-ID";

    private readonly EbayOptions _ebay = ebayOptions.Value;

    public Uri ApiUrl(string relativePath) => new(EbayEndpoints.ApiBaseUrl(_ebay.Environment), relativePath);

    public Uri MediaUrl(string relativePath) => new(EbayEndpoints.MediaBaseUrl(_ebay.Environment), relativePath);

    public Task<EbayResponse<TResponse>> GetAsync<TResponse>(Uri url, CancellationToken cancellationToken) =>
        SendAsync<TResponse>(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);

    public Task<EbayResponse<TResponse>> SendJsonAsync<TResponse>(HttpMethod method, Uri url, object body, CancellationToken cancellationToken)
    {
        // StringContent is buffered, so the resilience handler can resend it on retry.
        var json = JsonSerializer.Serialize(body, body.GetType(), EbayJson.Options);
        var request = new HttpRequestMessage(method, url) { Content = new StringContent(json, Encoding.UTF8, JsonMediaType) };

        return SendAsync<TResponse>(request, cancellationToken);
    }

    public async Task<EbayResponse<TResponse>> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            await AddStandardHeadersAsync(request, cancellationToken);

            var httpClient = httpClientFactory.CreateClient(HttpClientName);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return EbayResponse<TResponse>.Create(response.StatusCode, content, response.Headers.Location);
        }
    }

    private async Task AddStandardHeadersAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await accessTokenProvider.GetAccessTokenAsync(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
        request.Headers.Add(MarketplaceHeader, _ebay.MarketplaceId);
        request.Content?.Headers.ContentLanguage.Add(_ebay.ContentLanguage);
    }
}
