namespace EbaySellerTool.Core.Ebay.Inventory;

public interface IEbayLocationClient
{
    Task<IReadOnlyList<InventoryLocation>> GetLocationsAsync(CancellationToken cancellationToken);

    Task CreateLocationAsync(InventoryLocation location, CancellationToken cancellationToken);
}
