using System.Text.Json;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Inventory.Models;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Ebay;

public class EbayJsonTests
{
    [Fact]
    public void Serialize_InventoryItem_UsesEbayPropertyNames()
    {
        var item = TestSettings.CreateRequestMapper().MapInventoryItem(TestListings.Valid(), ["https://i.ebayimg.test/a.jpg"]);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new BulkRequest<InventoryItemRequest>([item]), EbayJson.Options));
        var request = json.RootElement.GetProperty("requests")[0];

        Assert.Equal(1, request.GetProperty("availability").GetProperty("shipToLocationAvailability").GetProperty("quantity").GetInt32());
        Assert.Equal("40001", request.GetProperty("conditionDescriptors")[0].GetProperty("name").GetString());
        Assert.Equal("https://i.ebayimg.test/a.jpg", request.GetProperty("product").GetProperty("imageUrls")[0].GetString());
    }

    [Fact]
    public void Serialize_OfferWithoutStoreCategory_OmitsNullProperty()
    {
        var offer = TestSettings.CreateRequestMapper().MapOffer(TestListings.Valid());

        var json = JsonSerializer.Serialize(offer, EbayJson.Options);

        Assert.DoesNotContain("storeCategoryNames", json);
        Assert.Contains("\"pricingSummary\":{\"price\":{\"value\":\"49.95\",\"currency\":\"AUD\"}}", json);
    }

    [Fact]
    public void Deserialize_BulkOfferResponse_ReadsErrors()
    {
        const string json = """
            {"statusCode":400,"sku":"A","errors":[{"errorId":25002,"domain":"API_INVENTORY","message":"Offer exists","parameters":[{"name":"offerId","value":"123"}]}]}
            """;

        var response = JsonSerializer.Deserialize<OfferResponse>(json, EbayJson.Options)!;

        Assert.False(response.IsSuccess);
        Assert.Equal(25002, Assert.Single(response.Errors).ErrorId);
        Assert.Equal("123", response.Errors[0].Parameters[0].Value);
    }
}
