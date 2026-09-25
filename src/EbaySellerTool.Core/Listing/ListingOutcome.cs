namespace EbaySellerTool.Core.Listing;

public sealed record ListingOutcome
{
    public required int RowNumber { get; init; }
    public string? Sku { get; init; }
    public string? Title { get; init; }
    public required ListingStatus Status { get; init; }
    public string? OfferId { get; init; }
    public string? ListingId { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public bool IsSuccess => Status is ListingStatus.Listed or ListingStatus.Revised or ListingStatus.DryRun;
}
