namespace EbaySellerTool.Core.Ebay.Stores;

public sealed class StoreCategoryCatalog(IEbayStoreClient storeClient, IStoreCategoryCache cache) : IStoreCategoryCatalog
{
    public async Task<IReadOnlyList<StoreCategory>> RefreshAsync(CancellationToken cancellationToken)
    {
        var categories = await storeClient.GetCategoriesAsync(cancellationToken);
        cache.Save(categories);

        return categories;
    }
}
