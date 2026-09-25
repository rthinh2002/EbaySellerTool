using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Core.Validation.Rules;

public sealed class QuantityRule : IListingRule
{
    public IEnumerable<ValidationError> Validate(CardListing listing)
    {
        if (listing.Quantity < 1)
        {
            yield return new ValidationError(listing.RowNumber, ListingColumns.Quantity.Header, "Quantity must be at least 1.");
        }
    }
}
