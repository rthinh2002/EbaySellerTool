using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Core.Validation.Rules;

public sealed class CategoryIdRule : IListingRule
{
    public IEnumerable<ValidationError> Validate(CardListing listing)
    {
        if (listing.CategoryId is not null && !listing.CategoryId.All(char.IsAsciiDigit))
        {
            yield return new ValidationError(
                listing.RowNumber,
                ListingColumns.CategoryId.Header,
                $"Category ID '{listing.CategoryId}' must be a number.");
        }
    }
}
