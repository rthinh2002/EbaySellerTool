using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Validation;

namespace EbaySellerTool.Core.Import;

public sealed record ListingImportResult(
    int TotalRows,
    IReadOnlyList<CardListing> ValidListings,
    IReadOnlyList<ValidationError> Errors)
{
    public bool HasErrors => Errors.Count > 0;

    public int InvalidRowCount => TotalRows - ValidListings.Count;

    public static ListingImportResult SheetFailure(IReadOnlyList<ValidationError> errors) => new(0, [], errors);
}
