namespace EbaySellerTool.Core.Ebay.Inventory.Models;

public sealed record ConditionDescriptor(string Name, IReadOnlyList<string> Values);
