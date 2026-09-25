namespace EbaySellerTool.Core.Cards;

public static class ListingLimits
{
    public const int MaxTitleLength = 80;
    public const int MaxSkuLength = 50;
    public const int MaxImagesPerListing = 24;

    public static IReadOnlySet<string> SupportedImageExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".heic", ".avif"
    };
}
