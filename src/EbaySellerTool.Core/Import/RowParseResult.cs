using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Validation;

namespace EbaySellerTool.Core.Import;

public sealed record RowParseResult(CardListing? Listing, IReadOnlyList<ValidationError> Errors)
{
    public static RowParseResult Success(CardListing listing) => new(listing, []);

    public static RowParseResult Failure(IReadOnlyList<ValidationError> errors) => new(null, errors);
}
