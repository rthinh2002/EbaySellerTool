using EbaySellerTool.Core.Ebay.Inventory.Models;

namespace EbaySellerTool.Core.Listing.Steps;

internal static class BulkResponseMatcher
{
    public const string MissingResponseMessage = "eBay returned no result for this item.";

    public static IEnumerable<(ListingJob Job, TResponse? Response)> Match<TResponse>(
        IEnumerable<ListingJob> jobs,
        IEnumerable<TResponse> responses,
        Func<ListingJob, string?> jobKey,
        Func<TResponse, string?> responseKey)
        where TResponse : BulkItemResponse
    {
        var responsesByKey = responses
            .Where(response => responseKey(response) is not null)
            .GroupBy(response => responseKey(response)!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        return jobs.Select(job => (job, jobKey(job) is { } key ? responsesByKey.GetValueOrDefault(key) : null));
    }
}
