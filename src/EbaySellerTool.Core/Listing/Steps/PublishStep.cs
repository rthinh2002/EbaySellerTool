using EbaySellerTool.Core.Ebay.Inventory;

namespace EbaySellerTool.Core.Listing.Steps;

public sealed class PublishStep(IEbayInventoryClient inventoryClient) : IListingStep
{
    private const string MissingOfferMessage = "No eBay offer was created for this item.";

    public async Task ExecuteAsync(IReadOnlyList<ListingJob> jobs, CancellationToken cancellationToken)
    {
        var publishableJobs = FailJobsWithoutOffer(jobs);

        if (publishableJobs.Count == 0)
        {
            return;
        }

        var offerIds = publishableJobs.Select(job => job.OfferId!).ToList();
        var responses = await inventoryClient.BulkPublishOffersAsync(offerIds, cancellationToken);

        foreach (var (job, response) in BulkResponseMatcher.Match(publishableJobs, responses, job => job.OfferId, response => response.OfferId))
        {
            if (response is { IsSuccess: true, ListingId: not null })
            {
                job.AddWarnings(response.Warnings);
                job.MarkListed(response.ListingId);
            }
            else if (response is null || response.Errors.Count == 0)
            {
                job.Fail(BulkResponseMatcher.MissingResponseMessage);
            }
            else
            {
                job.Fail(response.Errors);
            }
        }
    }

    private static List<ListingJob> FailJobsWithoutOffer(IReadOnlyList<ListingJob> jobs)
    {
        foreach (var job in jobs.Where(job => job.OfferId is null))
        {
            job.Fail(MissingOfferMessage);
        }

        return [.. jobs.Where(job => job.IsActive)];
    }
}
