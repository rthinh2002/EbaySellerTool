using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Core.Validation.Rules;

public sealed class DuplicateSkuRule : IListingBatchRule
{
    public IEnumerable<ValidationError> Validate(IReadOnlyList<CardListing> listings) =>
        listings
            .GroupBy(listing => listing.Sku, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(listing => new ValidationError(
                listing.RowNumber,
                ListingColumns.Sku.Header,
                $"SKU '{listing.Sku}' is used on rows {string.Join(", ", group.Select(duplicate => duplicate.RowNumber))}. Each SKU must be unique.")));
}
