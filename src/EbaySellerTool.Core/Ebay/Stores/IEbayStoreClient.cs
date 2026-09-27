namespace EbaySellerTool.Core.Ebay.Stores;

public interface IEbayStoreClient
{
    /// <summary>Every category in the seller's eBay Store, flattened in display order (parents before children).</summary>
    Task<IReadOnlyList<StoreCategory>> GetCategoriesAsync(CancellationToken cancellationToken);
}
