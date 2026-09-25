namespace EbaySellerTool.Core.Ebay.Inventory.Models;

public sealed record OfferResponse : BulkItemResponse
{
    public string Sku { get; init; } = string.Empty;
    public string? OfferId { get; init; }
}
