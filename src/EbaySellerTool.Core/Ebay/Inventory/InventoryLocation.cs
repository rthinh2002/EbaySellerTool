namespace EbaySellerTool.Core.Ebay.Inventory;

/// <summary>Where the seller ships from. Offers reference it by <see cref="MerchantLocationKey"/>.</summary>
public sealed record InventoryLocation(
    string MerchantLocationKey,
    string Name,
    string City,
    string StateOrProvince,
    string PostalCode,
    string Country);
