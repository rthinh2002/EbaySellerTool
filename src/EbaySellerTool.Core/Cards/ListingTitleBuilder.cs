namespace EbaySellerTool.Core.Cards;

public static class ListingTitleBuilder
{
    public static string Build(CardListing listing)
    {
        string?[] parts =
        [
            listing.CardName,
            listing.CardNumber,
            listing.Rarity,
            listing.SetName,
            listing.Game,
            listing.Condition.ToCode()
        ];

        var title = string.Empty;

        foreach (var part in parts.Where(part => !string.IsNullOrWhiteSpace(part)))
        {
            var candidate = title.Length == 0 ? part!.Trim() : $"{title} {part!.Trim()}";

            if (candidate.Length <= ListingLimits.MaxTitleLength)
            {
                title = candidate;
            }
        }

        return title;
    }
}
