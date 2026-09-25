namespace EbaySellerTool.Core.Ebay.Inventory.Models;

public sealed record InventoryItemResponse : BulkItemResponse
{
    public string Sku { get; init; } = string.Empty;
}
