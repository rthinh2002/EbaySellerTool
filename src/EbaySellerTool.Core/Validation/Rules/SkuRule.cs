using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Core.Validation.Rules;

public sealed class SkuRule : IListingRule
{
    public IEnumerable<ValidationError> Validate(CardListing listing)
    {
        if (listing.Sku.Length > ListingLimits.MaxSkuLength)
        {
            yield return new ValidationError(
                listing.RowNumber,
                ListingColumns.Sku.Header,
                $"SKU is {listing.Sku.Length} characters; the maximum is {ListingLimits.MaxSkuLength}.");
        }
    }
}
