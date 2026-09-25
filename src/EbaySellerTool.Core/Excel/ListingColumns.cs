using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Core.Excel;

public static class ListingColumns
{
    public const string AspectPrefix = "Aspect:";
    public const char ImagePathSeparator = '|';

    public static readonly ColumnDefinition Sku = new(
        "SKU",
        $"Unique ID for the listing (max {ListingLimits.MaxSkuLength} chars). Leave blank to generate from CardNumber, Rarity and CardCondition.",
        "LOB-EN001-UR-NM");

    public static readonly ColumnDefinition Title = new(
        "Title",
        $"Listing title (max {ListingLimits.MaxTitleLength} chars). Leave blank to generate from the card details.",
        "Blue-Eyes White Dragon LOB-EN001 Ultra Rare Yu-Gi-Oh! NM");

    public static readonly ColumnDefinition Game = new("Game", "Trading card game.", "Yu-Gi-Oh! TCG", IsRequired: true);

    public static readonly ColumnDefinition CardName = new("CardName", "Name of the card.", "Blue-Eyes White Dragon", IsRequired: true);

    public static readonly ColumnDefinition SetName = new("SetName", "Set or expansion.", "Legend of Blue Eyes White Dragon");

    public static readonly ColumnDefinition CardNumber = new("CardNumber", "Card number printed on the card.", "LOB-EN001");

    public static readonly ColumnDefinition Rarity = new("Rarity", "Card rarity.", "Ultra Rare");

    public static readonly ColumnDefinition Language = new("Language", "Card language.", "English");

    public static readonly ColumnDefinition CardCondition = new(
        "CardCondition",
        $"Raw card condition: {string.Join(", ", CardConditions.AllDisplayNames)}. Short codes NM, LP, MP and HP also work.",
        "Near Mint or Better",
        IsRequired: true);

    public static readonly ColumnDefinition Quantity = new("Quantity", "Quantity available. Defaults to 1.", "1");

    public static readonly ColumnDefinition Price = new("Price", "Price in AUD.", "49.95", IsRequired: true);

    public static readonly ColumnDefinition Images = new(
        "Images",
        $"Local image file paths separated by '{ImagePathSeparator}' (max {ListingLimits.MaxImagesPerListing}). The first image is the main photo. Relative paths are resolved from the Excel file's folder.",
        @"images\blue-eyes-front.jpg | images\blue-eyes-back.jpg",
        IsRequired: true);

    public static readonly ColumnDefinition StoreCategory = new("StoreCategory", "eBay Store category path.", "/Yu-Gi-Oh/Singles");

    public static readonly ColumnDefinition CategoryId = new("CategoryId", "eBay category ID. Leave blank to use the default category.", "183454");

    public static readonly ColumnDefinition Description = new("Description", "Listing description. Leave blank to use the default template.", "Pack fresh, sleeved straight away.");

    public static IReadOnlyList<ColumnDefinition> All { get; } =
    [
        Sku, Title, Game, CardName, SetName, CardNumber, Rarity, Language,
        CardCondition, Quantity, Price, Images, StoreCategory, CategoryId, Description
    ];

    public static IReadOnlyList<ColumnDefinition> Required { get; } = [.. All.Where(column => column.IsRequired)];

    public static int IndexOf(ColumnDefinition column) =>
        All.Select((candidate, index) => (candidate, index)).First(pair => pair.candidate == column).index;

    public static bool IsAspectHeader(string header) =>
        header.StartsWith(AspectPrefix, StringComparison.OrdinalIgnoreCase);

    public static string GetAspectName(string header) => header[AspectPrefix.Length..].Trim();

    public static string NormalizeHeader(string header) =>
        string.Concat(header.Where(character => !char.IsWhiteSpace(character) && character != '_')).ToUpperInvariant();
}
