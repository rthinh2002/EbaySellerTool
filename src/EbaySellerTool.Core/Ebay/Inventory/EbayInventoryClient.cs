using System.Net;
using EbaySellerTool.Core.Ebay.Http;
using EbaySellerTool.Core.Ebay.Inventory.Models;

namespace EbaySellerTool.Core.Ebay.Inventory;

public sealed class EbayInventoryClient(EbayRestClient restClient) : IEbayInventoryClient
{
    private const string InventoryPath = "sell/inventory/v1/";

    public Task<IReadOnlyList<InventoryItemResponse>> BulkCreateOrReplaceInventoryItemsAsync(
        IReadOnlyList<InventoryItemRequest> items, CancellationToken cancellationToken) =>
        PostBulkAsync<InventoryItemResponse>("bulk_create_or_replace_inventory_item", items, "Creating inventory items", cancellationToken);

    public Task<IReadOnlyList<OfferResponse>> BulkCreateOffersAsync(IReadOnlyList<OfferRequest> offers, CancellationToken cancellationToken) =>
        PostBulkAsync<OfferResponse>("bulk_create_offer", offers, "Creating offers", cancellationToken);

    public Task<IReadOnlyList<PublishOfferResponse>> BulkPublishOffersAsync(IReadOnlyList<string> offerIds, CancellationToken cancellationToken) =>
        PostBulkAsync<PublishOfferResponse>(
            "bulk_publish_offer",
            [.. offerIds.Select(offerId => new PublishOfferRequest(offerId))],
            "Publishing offers",
            cancellationToken);

    public async Task<ExistingOffer?> FindOfferAsync(string sku, CancellationToken cancellationToken)
    {
        var url = restClient.ApiUrl($"{InventoryPath}offer?sku={Uri.EscapeDataString(sku)}");
        var response = await restClient.GetAsync<OffersResponse>(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var offer = response.EnsureSuccess($"Looking up the offer for SKU '{sku}'").Value?.Offers?.FirstOrDefault();
        return offer is null ? null : new ExistingOffer(offer.OfferId, offer.Listing?.ListingId);
    }

    public async Task<IReadOnlyList<EbayError>> UpdateOfferAsync(string offerId, OfferRequest offer, CancellationToken cancellationToken)
    {
        // updateOffer replaces the whole offer but rejects the fields that identify it (sku, marketplaceId, format).
        var offerDetails = new
        {
            offer.AvailableQuantity,
            offer.CategoryId,
            offer.ListingDuration,
            offer.ListingPolicies,
            offer.PricingSummary,
            offer.MerchantLocationKey,
            offer.StoreCategoryNames
        };

        var url = restClient.ApiUrl($"{InventoryPath}offer/{Uri.EscapeDataString(offerId)}");
        var response = await restClient.SendJsonAsync<EmptyResponse>(HttpMethod.Put, url, offerDetails, cancellationToken);

        return response.IsSuccess ? [] : response.Errors;
    }

    private async Task<IReadOnlyList<TResponse>> PostBulkAsync<TResponse>(
        string method, IReadOnlyList<object> requests, string operation, CancellationToken cancellationToken)
    {
        var response = await restClient.SendJsonAsync<BulkResponse<TResponse>>(
            HttpMethod.Post, restClient.ApiUrl(InventoryPath + method), new BulkRequest<object>(requests), cancellationToken);

        if (response.Value?.Responses is { Count: > 0 } itemResponses)
        {
            return itemResponses;
        }

        return response.EnsureSuccess(operation).Value?.Responses ?? [];
    }

    private sealed record BulkResponse<TResponse>(IReadOnlyList<TResponse>? Responses);

    private sealed record OffersResponse(IReadOnlyList<OfferSummary>? Offers);

    private sealed record OfferSummary(string OfferId, OfferListing? Listing);

    private sealed record OfferListing(string? ListingId);
}
