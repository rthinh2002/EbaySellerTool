using EbaySellerTool.Core.Ebay.Inventory.Models;

namespace EbaySellerTool.Core.Ebay.Inventory;

/// <summary>
/// eBay Sell Inventory API. Bulk methods accept at most <see cref="EbayLimits.MaxBulkBatchSize"/> items and report
/// per-item failures in their responses; a failure of the whole request throws <see cref="EbayApiException"/>.
/// </summary>
public interface IEbayInventoryClient
{
    Task<IReadOnlyList<InventoryItemResponse>> BulkCreateOrReplaceInventoryItemsAsync(
        IReadOnlyList<InventoryItemRequest> items, CancellationToken cancellationToken);

    Task<IReadOnlyList<OfferResponse>> BulkCreateOffersAsync(
        IReadOnlyList<OfferRequest> offers, CancellationToken cancellationToken);

    Task<IReadOnlyList<PublishOfferResponse>> BulkPublishOffersAsync(
        IReadOnlyList<string> offerIds, CancellationToken cancellationToken);

    Task<ExistingOffer?> FindOfferAsync(string sku, CancellationToken cancellationToken);

    /// <returns>The errors eBay reported, or an empty list when the update succeeded.</returns>
    Task<IReadOnlyList<EbayError>> UpdateOfferAsync(string offerId, OfferRequest offer, CancellationToken cancellationToken);
}
