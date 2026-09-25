using EbaySellerTool.Core.Ebay.Inventory.Models;

namespace EbaySellerTool.Core.Listing.DryRun;

public sealed record DryRunBatch(
    int BatchNumber,
    BulkRequest<InventoryItemRequest> InventoryItems,
    BulkRequest<OfferRequest> Offers);
