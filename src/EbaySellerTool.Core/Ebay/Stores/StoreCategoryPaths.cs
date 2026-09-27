namespace EbaySellerTool.Core.Ebay.Stores;

public static class StoreCategoryPaths
{
    public const string Separator = "/";

    /// <summary>Store category paths start with "/"; sheet values may leave it out.</summary>
    public static string Normalize(string storeCategory)
    {
        var trimmed = storeCategory.Trim();
        return trimmed.StartsWith(Separator, StringComparison.Ordinal) ? trimmed : Separator + trimmed;
    }
}
