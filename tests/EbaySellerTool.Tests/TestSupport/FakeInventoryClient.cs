using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Inventory;
using EbaySellerTool.Core.Ebay.Inventory.Models;

namespace EbaySellerTool.Tests.TestSupport;

/// <summary>
/// In-memory eBay Inventory API. New offers get ID "offer-{sku}" and publish as listing "listing-{sku}".
/// Configure per-SKU failures and pre-existing offers through the public dictionaries.
/// </summary>
internal sealed class FakeInventoryClient : IEbayInventoryClient
{
    private const int SuccessStatus = 200;
    private const int BadRequestStatus = 400;

    public Dictionary<string, EbayError> InventoryItemErrors { get; } = new();
    public Dictionary<string, EbayError> OfferCreateErrors { get; } = new();
    public Dictionary<string, EbayError> PublishErrors { get; } = new();
    public Dictionary<string, ExistingOffer> ExistingOffers { get; } = new();
    public Dictionary<string, EbayError> OfferUpdateErrors { get; } = new();

    public int? FailInventoryCallNumber { get; set; }

    public List<IReadOnlyList<InventoryItemRequest>> InventoryItemCalls { get; } = [];
    public List<IReadOnlyList<OfferRequest>> OfferCreateCalls { get; } = [];
    public List<IReadOnlyList<string>> PublishCalls { get; } = [];
    public List<(string OfferId, OfferRequest Offer)> OfferUpdates { get; } = [];

    public static EbayError Error(int errorId, string message) => new() { ErrorId = errorId, Message = message };

    public static string OfferIdFor(string sku) => $"offer-{sku}";

    public static string ListingIdFor(string sku) => $"listing-{sku}";

    public Task<IReadOnlyList<InventoryItemResponse>> BulkCreateOrReplaceInventoryItemsAsync(
        IReadOnlyList<InventoryItemRequest> items, CancellationToken cancellationToken)
    {
        InventoryItemCalls.Add(items);

        if (InventoryItemCalls.Count == FailInventoryCallNumber)
        {
            throw new EbayApiException("eBay is unavailable (503).");
        }

        IReadOnlyList<InventoryItemResponse> responses =
        [
            .. items.Select(item => new InventoryItemResponse
            {
                Sku = item.Sku,
                StatusCode = InventoryItemErrors.ContainsKey(item.Sku) ? BadRequestStatus : SuccessStatus,
                Errors = InventoryItemErrors.TryGetValue(item.Sku, out var error) ? [error] : []
            })
        ];
        return Task.FromResult(responses);
    }

    public Task<IReadOnlyList<OfferResponse>> BulkCreateOffersAsync(
        IReadOnlyList<OfferRequest> offers, CancellationToken cancellationToken)
    {
        OfferCreateCalls.Add(offers);

        IReadOnlyList<OfferResponse> responses = [.. offers.Select(CreateOfferResponse)];
        return Task.FromResult(responses);
    }

    public Task<IReadOnlyList<PublishOfferResponse>> BulkPublishOffersAsync(
        IReadOnlyList<string> offerIds, CancellationToken cancellationToken)
    {
        PublishCalls.Add(offerIds);

        IReadOnlyList<PublishOfferResponse> responses = [.. offerIds.Select(CreatePublishResponse)];
        return Task.FromResult(responses);
    }

    public Task<ExistingOffer?> FindOfferAsync(string sku, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingOffers.GetValueOrDefault(sku));

    public Task<IReadOnlyList<EbayError>> UpdateOfferAsync(string offerId, OfferRequest offer, CancellationToken cancellationToken)
    {
        OfferUpdates.Add((offerId, offer));

        IReadOnlyList<EbayError> errors = OfferUpdateErrors.TryGetValue(offer.Sku, out var error) ? [error] : [];
        return Task.FromResult(errors);
    }

    private OfferResponse CreateOfferResponse(OfferRequest offer) =>
        OfferCreateErrors.TryGetValue(offer.Sku, out var error)
            ? new OfferResponse { Sku = offer.Sku, StatusCode = BadRequestStatus, Errors = [error] }
            : new OfferResponse { Sku = offer.Sku, StatusCode = SuccessStatus, OfferId = OfferIdFor(offer.Sku) };

    private PublishOfferResponse CreatePublishResponse(string offerId)
    {
        var sku = offerId.StartsWith("offer-", StringComparison.Ordinal) ? offerId["offer-".Length..] : offerId;

        return PublishErrors.TryGetValue(sku, out var error)
            ? new PublishOfferResponse { OfferId = offerId, StatusCode = BadRequestStatus, Errors = [error] }
            : new PublishOfferResponse { OfferId = offerId, StatusCode = SuccessStatus, ListingId = ListingIdFor(sku) };
    }
}
