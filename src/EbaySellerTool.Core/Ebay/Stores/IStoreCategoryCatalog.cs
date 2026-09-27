namespace EbaySellerTool.Core.Ebay.Stores;

public interface IStoreCategoryCatalog
{
    /// <summary>Fetches the store categories from eBay and saves them for offline use.</summary>
    Task<IReadOnlyList<StoreCategory>> RefreshAsync(CancellationToken cancellationToken);
}
