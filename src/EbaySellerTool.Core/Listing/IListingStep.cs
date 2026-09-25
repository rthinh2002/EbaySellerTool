namespace EbaySellerTool.Core.Listing;

/// <summary>One stage of the listing pipeline, run on a batch of still-active jobs.</summary>
public interface IListingStep
{
    Task ExecuteAsync(IReadOnlyList<ListingJob> jobs, CancellationToken cancellationToken);
}
