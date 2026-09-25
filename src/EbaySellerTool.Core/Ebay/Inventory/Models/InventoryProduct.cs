namespace EbaySellerTool.Core.Ebay.Inventory.Models;

public sealed record InventoryProduct(
    string Title,
    string Description,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Aspects,
    IReadOnlyList<string> ImageUrls);
