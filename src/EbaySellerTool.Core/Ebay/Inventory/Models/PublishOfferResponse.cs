namespace EbaySellerTool.Core.Ebay.Inventory.Models;

public sealed record PublishOfferResponse : BulkItemResponse
{
    public string OfferId { get; init; } = string.Empty;
    public string? ListingId { get; init; }
}
