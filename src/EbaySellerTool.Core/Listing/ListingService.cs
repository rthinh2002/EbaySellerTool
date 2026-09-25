using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Ebay;

namespace EbaySellerTool.Core.Listing;

public sealed class ListingService(IEnumerable<IListingStep> steps) : IListingService
{
    private readonly IReadOnlyList<IListingStep> _steps = [.. steps];

    public async Task<ListingRunResult> ListAsync(IReadOnlyList<CardListing> listings, CancellationToken cancellationToken)
    {
        var jobs = listings.Select(listing => new ListingJob(listing)).ToList();

        foreach (var batch in jobs.Chunk(EbayLimits.MaxBulkBatchSize))
        {
            await RunStepsAsync(batch, cancellationToken);
        }

        return new ListingRunResult(jobs.Select(job => job.ToOutcome()));
    }

    private async Task RunStepsAsync(IReadOnlyList<ListingJob> batch, CancellationToken cancellationToken)
    {
        foreach (var step in _steps)
        {
            var activeJobs = batch.Where(job => job.IsActive).ToList();

            if (activeJobs.Count == 0)
            {
                return;
            }

            try
            {
                await step.ExecuteAsync(activeJobs, cancellationToken);
            }
            catch (EbayApiException exception)
            {
                activeJobs.ForEach(job => job.Fail(exception.Message));
            }
        }
    }
}
