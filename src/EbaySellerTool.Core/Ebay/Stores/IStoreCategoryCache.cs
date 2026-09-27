namespace EbaySellerTool.Core.Ebay.Stores;

/// <summary>
/// The last known store categories, kept locally so sheets can be validated and templates built without calling eBay.
/// </summary>
public interface IStoreCategoryCache
{
    /// <returns>The saved categories, or null when they have never been fetched.</returns>
    IReadOnlyList<StoreCategory>? Load();

    void Save(IReadOnlyList<StoreCategory> categories);
}
