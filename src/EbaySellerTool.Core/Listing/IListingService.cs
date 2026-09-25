using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Core.Listing;

public interface IListingService
{
    Task<ListingRunResult> ListAsync(IReadOnlyList<CardListing> listings, CancellationToken cancellationToken);
}
