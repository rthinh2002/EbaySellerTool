using EbaySellerTool.Core.Ebay.Inventory;

namespace EbaySellerTool.Core.Listing.Steps;

public sealed class InventoryItemStep(IEbayInventoryClient inventoryClient, IListingRequestMapper requestMapper) : IListingStep
{
    public async Task ExecuteAsync(IReadOnlyList<ListingJob> jobs, CancellationToken cancellationToken)
    {
        var requests = jobs.Select(job => requestMapper.MapInventoryItem(job.Listing, job.ImageUrls)).ToList();
        var responses = await inventoryClient.BulkCreateOrReplaceInventoryItemsAsync(requests, cancellationToken);

        foreach (var (job, response) in BulkResponseMatcher.Match(jobs, responses, job => job.Sku, response => response.Sku))
        {
            if (response is null)
            {
                job.Fail(BulkResponseMatcher.MissingResponseMessage);
            }
            else if (response.IsSuccess)
            {
                job.AddWarnings(response.Warnings);
            }
            else
            {
                job.Fail(response.Errors);
            }
        }
    }
}
