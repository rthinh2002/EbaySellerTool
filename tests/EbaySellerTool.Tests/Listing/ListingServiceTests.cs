using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Ebay.Inventory.Models;
using EbaySellerTool.Core.Listing;
using EbaySellerTool.Core.Listing.Steps;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Listing;

public class ListingServiceTests
{
    private readonly FakeInventoryClient _inventoryClient = new();
    private readonly FakeImageUploader _imageUploader = new();

    [Fact]
    public async Task ListAsync_AllSucceed_ListsEveryCard()
    {
        var result = await ListAsync(Card("A", 2), Card("B", 3));

        Assert.All(result.Outcomes, outcome => Assert.Equal(ListingStatus.Listed, outcome.Status));
        Assert.Equal(FakeInventoryClient.ListingIdFor("A"), result.Outcomes[0].ListingId);
        Assert.Equal(FakeInventoryClient.OfferIdFor("B"), result.Outcomes[1].OfferId);
    }

    [Fact]
    public async Task ListAsync_UploadsImagesAndSendsEbayUrls()
    {
        await ListAsync(Card("A", 2, "front.jpg", "back.jpg"));

        var sentImageUrls = _inventoryClient.InventoryItemCalls.Single().Single().Product.ImageUrls;
        Assert.Equal(["https://i.ebayimg.test/front.jpg", "https://i.ebayimg.test/back.jpg"], sentImageUrls);
    }

    [Fact]
    public async Task ListAsync_ImageUploadFails_FailsOnlyThatCard()
    {
        _imageUploader.FailingFileNames.Add("bad.jpg");

        var result = await ListAsync(Card("A", 2, "bad.jpg"), Card("B", 3));

        Assert.Equal(ListingStatus.Failed, result.Outcomes[0].Status);
        Assert.Contains("bad.jpg", Assert.Single(result.Outcomes[0].Errors));
        Assert.Equal(ListingStatus.Listed, result.Outcomes[1].Status);
        Assert.Equal(["B"], _inventoryClient.InventoryItemCalls.Single().Select(item => item.Sku));
    }

    [Fact]
    public async Task ListAsync_InventoryItemRejected_ReportsEbayErrorAndSkipsOffer()
    {
        _inventoryClient.InventoryItemErrors["A"] = FakeInventoryClient.Error(25604, "Missing item specific Game");

        var result = await ListAsync(Card("A", 2), Card("B", 3));

        Assert.Equal(ListingStatus.Failed, result.Outcomes[0].Status);
        Assert.Equal("Missing item specific Game [eBay 25604]", Assert.Single(result.Outcomes[0].Errors));
        Assert.Equal(["B"], _inventoryClient.OfferCreateCalls.Single().Select(offer => offer.Sku));
    }

    [Fact]
    public async Task ListAsync_OfferCreateFailsWithoutExistingOffer_ReportsCreateError()
    {
        _inventoryClient.OfferCreateErrors["A"] = FakeInventoryClient.Error(25002, "Invalid category");

        var result = await ListAsync(Card("A", 2));

        Assert.Equal(ListingStatus.Failed, result.Outcomes[0].Status);
        Assert.Contains("Invalid category", result.Outcomes[0].Errors[0]);
        Assert.Empty(_inventoryClient.PublishCalls);
    }

    [Fact]
    public async Task ListAsync_OfferAlreadyLive_UpdatesItAndMarksRevised()
    {
        _inventoryClient.OfferCreateErrors["A"] = FakeInventoryClient.Error(25002, "Offer entity already exists");
        _inventoryClient.ExistingOffers["A"] = new ExistingOffer("existing-offer", "existing-listing");

        var result = await ListAsync(Card("A", 2));

        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(ListingStatus.Revised, outcome.Status);
        Assert.Equal("existing-listing", outcome.ListingId);
        Assert.Equal("existing-offer", Assert.Single(_inventoryClient.OfferUpdates).OfferId);
        Assert.Empty(_inventoryClient.PublishCalls);
    }

    [Fact]
    public async Task ListAsync_OfferExistsButUnpublished_UpdatesThenPublishes()
    {
        _inventoryClient.OfferCreateErrors["A"] = FakeInventoryClient.Error(25002, "Offer entity already exists");
        _inventoryClient.ExistingOffers["A"] = new ExistingOffer("offer-A", ListingId: null);

        var result = await ListAsync(Card("A", 2));

        Assert.Equal(ListingStatus.Listed, Assert.Single(result.Outcomes).Status);
        Assert.Single(_inventoryClient.OfferUpdates);
        Assert.Equal(["offer-A"], _inventoryClient.PublishCalls.Single());
    }

    [Fact]
    public async Task ListAsync_ExistingOfferUpdateFails_ReportsUpdateError()
    {
        _inventoryClient.OfferCreateErrors["A"] = FakeInventoryClient.Error(25002, "Offer entity already exists");
        _inventoryClient.ExistingOffers["A"] = new ExistingOffer("offer-A", null);
        _inventoryClient.OfferUpdateErrors["A"] = FakeInventoryClient.Error(25709, "Invalid price");

        var result = await ListAsync(Card("A", 2));

        Assert.Contains("Invalid price", Assert.Single(result.Outcomes[0].Errors));
    }

    [Fact]
    public async Task ListAsync_PublishRejected_ReportsErrorAndKeepsOfferId()
    {
        _inventoryClient.PublishErrors["A"] = FakeInventoryClient.Error(25007, "Invalid fulfillment policy");

        var result = await ListAsync(Card("A", 2));

        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(ListingStatus.Failed, outcome.Status);
        Assert.Equal(FakeInventoryClient.OfferIdFor("A"), outcome.OfferId);
        Assert.Contains("Invalid fulfillment policy", outcome.Errors[0]);
    }

    [Fact]
    public async Task ListAsync_MoreThanOneBatch_SendsBatchesOfAtMost25()
    {
        var cards = Enumerable.Range(1, 30).Select(index => Card($"SKU{index}", index + 1)).ToArray();

        var result = await ListAsync(cards);

        Assert.Equal([25, 5], _inventoryClient.InventoryItemCalls.Select(call => call.Count));
        Assert.Equal([25, 5], _inventoryClient.PublishCalls.Select(call => call.Count));
        Assert.Equal(30, result.Count(ListingStatus.Listed));
    }

    [Fact]
    public async Task ListAsync_WholeRequestFails_FailsThatBatchAndContinues()
    {
        _inventoryClient.FailInventoryCallNumber = 1;
        var cards = Enumerable.Range(1, 26).Select(index => Card($"SKU{index}", index + 1)).ToArray();

        var result = await ListAsync(cards);

        Assert.Equal(25, result.Count(ListingStatus.Failed));
        Assert.Contains("eBay is unavailable", result.Outcomes[0].Errors[0]);
        Assert.Equal(ListingStatus.Listed, result.Outcomes[25].Status);
    }

    private Task<ListingRunResult> ListAsync(params CardListing[] listings)
    {
        var mapper = TestSettings.CreateRequestMapper();
        var service = new ListingService(
        [
            new ImageUploadStep(_imageUploader),
            new InventoryItemStep(_inventoryClient, mapper),
            new OfferStep(_inventoryClient, mapper),
            new PublishStep(_inventoryClient)
        ]);

        return service.ListAsync(listings, CancellationToken.None);
    }

    private static CardListing Card(string sku, int rowNumber, params string[] imageFileNames) =>
        TestListings.Valid(rowNumber) with
        {
            Sku = sku,
            ImagePaths = [.. (imageFileNames.Length == 0 ? ["front.jpg"] : imageFileNames).Select(name => Path.Combine(@"C:\cards", name))]
        };
}
