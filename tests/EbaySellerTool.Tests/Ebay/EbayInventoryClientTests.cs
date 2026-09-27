using System.Net;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Inventory;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Ebay;

public class EbayInventoryClientTests
{
    private readonly EbayClientTestContext _context = new();
    private readonly EbayInventoryClient _client;

    public EbayInventoryClientTests()
    {
        _client = new EbayInventoryClient(_context.RestClient);
    }

    [Fact]
    public async Task BulkCreateOffersAsync_SendsSignedRequestWithMarketplaceAndLanguageHeaders()
    {
        _context.Http.RespondWith(HttpStatusCode.OK, """{"responses":[{"statusCode":200,"sku":"A","offerId":"1"}]}""");
        var offer = TestSettings.CreateRequestMapper().MapOffer(TestListings.Valid() with { Sku = "A" });

        var responses = await _client.BulkCreateOffersAsync([offer], CancellationToken.None);

        var (request, body) = Assert.Single(_context.Http.Requests);
        Assert.Equal("https://api.sandbox.ebay.com/sell/inventory/v1/bulk_create_offer", request.RequestUri!.AbsoluteUri);
        Assert.Equal($"Bearer {EbayClientTestContext.AccessToken}", request.Headers.Authorization!.ToString());
        Assert.Equal("EBAY_AU", request.Headers.GetValues("X-EBAY-C-MARKETPLACE-ID").Single());
        Assert.Equal("en-AU", request.Content!.Headers.ContentLanguage.Single());
        Assert.StartsWith("""{"requests":[{"sku":"A","marketplaceId":"EBAY_AU""", body);
        Assert.Equal("1", Assert.Single(responses).OfferId);
    }

    [Fact]
    public async Task BulkCreateOffersAsync_AllItemsFailWithHttp400_ReturnsPerItemErrors()
    {
        _context.Http.RespondWith(HttpStatusCode.BadRequest, """
            {"responses":[{"statusCode":400,"sku":"A","errors":[{"errorId":25002,"message":"Offer entity already exists."}]}]}
            """);

        var responses = await _client.BulkCreateOffersAsync([], CancellationToken.None);

        Assert.Equal(25002, Assert.Single(Assert.Single(responses).Errors).ErrorId);
    }

    [Fact]
    public async Task BulkPublishOffersAsync_RequestFailsWithoutItemResults_Throws()
    {
        _context.Http.RespondWith(HttpStatusCode.Unauthorized, """{"errors":[{"errorId":1001,"message":"Invalid access token"}]}""");

        var exception = await Assert.ThrowsAsync<EbayApiException>(() => _client.BulkPublishOffersAsync(["1"], CancellationToken.None));

        Assert.Contains("Invalid access token", exception.Message);
    }

    [Fact]
    public async Task FindOfferAsync_PublishedOffer_ReturnsOfferAndListingIds()
    {
        _context.Http.RespondWith(HttpStatusCode.OK, """
            {"total":1,"offers":[{"offerId":"117","sku":"A","status":"PUBLISHED","listing":{"listingId":"110","listingStatus":"ACTIVE"}}]}
            """);

        var offer = await _client.FindOfferAsync("A B", CancellationToken.None);

        Assert.Equal(new Core.Ebay.Inventory.Models.ExistingOffer("117", "110"), offer);
        Assert.EndsWith("offer?sku=A%20B", _context.Http.Requests.Single().Request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task FindOfferAsync_NoOffer_ReturnsNull()
    {
        _context.Http.RespondWith(HttpStatusCode.NotFound, """{"errors":[{"errorId":25713,"message":"This Offer is not available."}]}""");

        Assert.Null(await _client.FindOfferAsync("A", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateOfferAsync_SendsOfferWithoutIdentifyingFields()
    {
        _context.Http.RespondWith(HttpStatusCode.NoContent, string.Empty);
        var offer = TestSettings.CreateRequestMapper().MapOffer(TestListings.Valid());

        var errors = await _client.UpdateOfferAsync("117", offer, CancellationToken.None);

        var (request, body) = _context.Http.Requests.Single();
        Assert.Empty(errors);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith("sell/inventory/v1/offer/117", request.RequestUri!.AbsoluteUri);
        Assert.DoesNotContain("\"sku\"", body);
        Assert.DoesNotContain("\"marketplaceId\"", body);
        Assert.DoesNotContain("\"format\"", body);
        Assert.Contains("\"pricingSummary\"", body);
    }
}
