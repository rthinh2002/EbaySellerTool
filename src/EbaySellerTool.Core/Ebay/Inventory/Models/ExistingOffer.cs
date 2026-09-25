namespace EbaySellerTool.Core.Ebay.Inventory.Models;

/// <param name="ListingId">Set when the offer is already published as a live listing.</param>
public sealed record ExistingOffer(string OfferId, string? ListingId);
