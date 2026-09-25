namespace EbaySellerTool.Core.Ebay.Inventory.Models;

public sealed record InventoryItemRequest(
    string Sku,
    string Locale,
    InventoryProduct Product,
    string Condition,
    IReadOnlyList<ConditionDescriptor> ConditionDescriptors,
    InventoryAvailability Availability);
