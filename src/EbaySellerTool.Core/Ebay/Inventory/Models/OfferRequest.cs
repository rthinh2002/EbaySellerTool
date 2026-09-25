namespace EbaySellerTool.Core.Ebay.Inventory.Models;

public sealed record OfferRequest(
    string Sku,
    string MarketplaceId,
    string Format,
    string ListingDuration,
    int AvailableQuantity,
    string CategoryId,
    ListingPolicies ListingPolicies,
    PricingSummary PricingSummary,
    string MerchantLocationKey,
    IReadOnlyList<string>? StoreCategoryNames);
