using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Core.Validation.Rules;

public sealed class TitleRule : IListingRule
{
    public IEnumerable<ValidationError> Validate(CardListing listing)
    {
        if (listing.Title.Length > ListingLimits.MaxTitleLength)
        {
            yield return new ValidationError(
                listing.RowNumber,
                ListingColumns.Title.Header,
                $"Title is {listing.Title.Length} characters; the maximum is {ListingLimits.MaxTitleLength}.");
        }
    }
}
