using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Inventory;

namespace EbaySellerTool.Core.Listing.DryRun;

/// <summary>
/// Builds the exact bulk requests a live run would send, without calling eBay. Images are only uploaded
/// during a live run, so local file URIs stand in for the eBay-hosted image URLs.
/// </summary>
public sealed class DryRunPlanner(IListingRequestMapper requestMapper) : IDryRunPlanner
{
    public DryRunPlan CreatePlan(IReadOnlyList<CardListing> listings) =>
        new([.. listings.Chunk(EbayLimits.MaxBulkBatchSize).Select(CreateBatch)]);

    private DryRunBatch CreateBatch(CardListing[] listings, int index) => new(
        BatchNumber: index + 1,
        InventoryItems: new([.. listings.Select(listing => requestMapper.MapInventoryItem(listing, ToPlaceholderImageUrls(listing)))]),
        Offers: new([.. listings.Select(requestMapper.MapOffer)]));

    private static IReadOnlyList<string> ToPlaceholderImageUrls(CardListing listing) =>
        [.. listing.ImagePaths.Select(path => new Uri(path).AbsoluteUri)];
}
