namespace EbaySellerTool.Core.Listing;

public sealed record ListingRunResult
{
    public ListingRunResult(IEnumerable<ListingOutcome> outcomes)
    {
        Outcomes = [.. outcomes.OrderBy(outcome => outcome.RowNumber)];
    }

    public IReadOnlyList<ListingOutcome> Outcomes { get; }

    public bool HasFailures => Outcomes.Any(outcome => !outcome.IsSuccess);

    public int Count(ListingStatus status) => Outcomes.Count(outcome => outcome.Status == status);
}
