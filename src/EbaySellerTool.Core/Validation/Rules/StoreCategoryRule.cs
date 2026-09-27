using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Ebay.Stores;
using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Core.Validation.Rules;

/// <summary>
/// Checks the StoreCategory column against the saved copy of the seller's store categories.
/// Skipped until the categories have been fetched at least once.
/// </summary>
public sealed class StoreCategoryRule(IStoreCategoryCache storeCategoryCache) : IListingRule
{
    private readonly Lazy<IReadOnlyList<string>?> _knownPaths = new(() =>
        storeCategoryCache.Load()?.Select(category => category.Path).ToList());

    public IEnumerable<ValidationError> Validate(CardListing listing)
    {
        if (listing.StoreCategory is null || _knownPaths.Value is not { Count: > 0 } knownPaths)
        {
            yield break;
        }

        var path = StoreCategoryPaths.Normalize(listing.StoreCategory);

        if (knownPaths.Contains(path, StringComparer.Ordinal))
        {
            yield break;
        }

        yield return new ValidationError(listing.RowNumber, ListingColumns.StoreCategory.Header, DescribeUnknownPath(path, knownPaths));
    }

    private static string DescribeUnknownPath(string path, IReadOnlyList<string> knownPaths)
    {
        var sameIgnoringCase = knownPaths.FirstOrDefault(known => string.Equals(known, path, StringComparison.OrdinalIgnoreCase));

        return sameIgnoringCase is not null
            ? $"Store category '{path}' should be written exactly as '{sameIgnoringCase}'."
            : $"Store category '{path}' isn't in your eBay Store. Use one of: {string.Join(", ", knownPaths)}.";
    }
}
