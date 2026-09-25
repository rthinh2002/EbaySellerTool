using EbaySellerTool.Core.Listing.DryRun;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Listing;

public class DryRunPlannerTests
{
    private readonly DryRunPlanner _planner = new(TestSettings.CreateRequestMapper());

    [Fact]
    public void CreatePlan_SplitsListingsIntoBatchesOf25()
    {
        var listings = Enumerable.Range(1, 60).Select(index => TestListings.Valid(index) with { Sku = $"SKU{index}" }).ToList();

        var plan = _planner.CreatePlan(listings);

        Assert.Equal([1, 2, 3], plan.Batches.Select(batch => batch.BatchNumber));
        Assert.Equal([25, 25, 10], plan.Batches.Select(batch => batch.InventoryItems.Requests.Count));
        Assert.Equal([25, 25, 10], plan.Batches.Select(batch => batch.Offers.Requests.Count));
    }

    [Fact]
    public void CreatePlan_UsesLocalFileUrisAsImagePlaceholders()
    {
        var listing = TestListings.Valid() with { ImagePaths = [@"C:\cards\blue eyes.jpg"] };

        var plan = _planner.CreatePlan([listing]);

        var imageUrl = plan.Batches.Single().InventoryItems.Requests.Single().Product.ImageUrls.Single();
        Assert.Equal("file:///C:/cards/blue%20eyes.jpg", imageUrl);
    }
}
