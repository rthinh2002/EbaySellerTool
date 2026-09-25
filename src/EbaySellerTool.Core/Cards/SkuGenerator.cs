using System.Text;

namespace EbaySellerTool.Core.Cards;

/// <summary>
/// Builds a deterministic SKU from the card's printing and condition, so re-running the same sheet
/// updates the existing eBay inventory item instead of creating a duplicate.
/// </summary>
public static class SkuGenerator
{
    private const char Separator = '-';

    public static string? Generate(CardListing listing)
    {
        if (string.IsNullOrWhiteSpace(listing.CardNumber))
        {
            return null;
        }

        string[] parts = [listing.CardNumber, listing.Rarity ?? string.Empty, listing.Condition.ToCode()];
        var sku = string.Join(Separator, parts.Select(Slugify).Where(part => part.Length > 0));

        return sku.Length <= ListingLimits.MaxSkuLength ? sku : sku[..ListingLimits.MaxSkuLength];
    }

    private static string Slugify(string text)
    {
        var slug = new StringBuilder(text.Length);

        foreach (var character in text.Trim().ToUpperInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                slug.Append(character);
            }
            else if (slug.Length > 0 && slug[^1] != Separator)
            {
                slug.Append(Separator);
            }
        }

        return slug.ToString().TrimEnd(Separator);
    }
}
