using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Core.Validation.Rules;

public sealed class PriceRule : IListingRule
{
    private const int MaxDecimalPlaces = 2;

    public IEnumerable<ValidationError> Validate(CardListing listing)
    {
        if (listing.Price <= 0)
        {
            yield return Error(listing, "Price must be greater than 0.");
        }
        else if (decimal.Round(listing.Price, MaxDecimalPlaces) != listing.Price)
        {
            yield return Error(listing, $"Price can have at most {MaxDecimalPlaces} decimal places.");
        }
    }

    private static ValidationError Error(CardListing listing, string message) =>
        new(listing.RowNumber, ListingColumns.Price.Header, message);
}
