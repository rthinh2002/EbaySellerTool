using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Inventory;
using EbaySellerTool.Core.Ebay.Inventory.Models;

namespace EbaySellerTool.Core.Listing.Steps;

/// <summary>
/// Creates an offer per SKU. When creation fails because the SKU already has an offer (a re-run of the same sheet),
/// the existing offer is updated instead; if it is already live, the update revises the listing directly.
/// </summary>
public sealed class OfferStep(IEbayInventoryClient inventoryClient, IListingRequestMapper requestMapper) : IListingStep
{
    public async Task ExecuteAsync(IReadOnlyList<ListingJob> jobs, CancellationToken cancellationToken)
    {
        var requestsBySku = jobs.ToDictionary(job => job.Sku, job => requestMapper.MapOffer(job.Listing), StringComparer.Ordinal);
        var responses = await inventoryClient.BulkCreateOffersAsync([.. requestsBySku.Values], cancellationToken);

        foreach (var (job, response) in BulkResponseMatcher.Match(jobs, responses, job => job.Sku, response => response.Sku))
        {
            if (response is { IsSuccess: true, OfferId: not null })
            {
                job.SetOfferId(response.OfferId);
                job.AddWarnings(response.Warnings);
            }
            else
            {
                await UpdateExistingOfferAsync(job, requestsBySku[job.Sku], response, cancellationToken);
            }
        }
    }

    private async Task UpdateExistingOfferAsync(
        ListingJob job, OfferRequest request, OfferResponse? failedCreateResponse, CancellationToken cancellationToken)
    {
        var existingOffer = await inventoryClient.FindOfferAsync(job.Sku, cancellationToken);

        if (existingOffer is null)
        {
            FailWithCreateErrors(job, failedCreateResponse);
            return;
        }

        var updateErrors = await inventoryClient.UpdateOfferAsync(existingOffer.OfferId, request, cancellationToken);

        if (updateErrors.Count > 0)
        {
            job.Fail(updateErrors);
            return;
        }

        job.SetOfferId(existingOffer.OfferId);

        if (existingOffer.ListingId is not null)
        {
            job.MarkRevised(existingOffer.ListingId);
        }
    }

    private static void FailWithCreateErrors(ListingJob job, OfferResponse? failedCreateResponse)
    {
        if (failedCreateResponse is null || failedCreateResponse.Errors.Count == 0)
        {
            job.Fail(BulkResponseMatcher.MissingResponseMessage);
        }
        else
        {
            job.Fail(failedCreateResponse.Errors);
        }
    }
}
