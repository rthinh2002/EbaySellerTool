using System.Globalization;
using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Descriptions;
using EbaySellerTool.Core.Ebay.Inventory.Models;
using EbaySellerTool.Core.Excel;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Core.Ebay.Inventory;

public sealed class ListingRequestMapper(
    IOptions<EbayOptions> ebayOptions,
    IOptions<ListingDefaultsOptions> listingDefaults,
    IListingDescriptionBuilder descriptionBuilder) : IListingRequestMapper
{
    private const string FixedPriceFormat = "FIXED_PRICE";
    private const string GoodTilCancelledDuration = "GTC";
    private const string StoreCategoryPathSeparator = "/";

    private readonly EbayOptions _ebay = ebayOptions.Value;
    private readonly ListingDefaultsOptions _defaults = listingDefaults.Value;

    public InventoryItemRequest MapInventoryItem(CardListing listing, IReadOnlyList<string> imageUrls) => new(
        Sku: listing.Sku,
        Locale: _ebay.Locale,
        Product: new InventoryProduct(listing.Title, descriptionBuilder.Build(listing), BuildAspects(listing), imageUrls),
        Condition: EbayCardConditions.UngradedCondition,
        ConditionDescriptors: [EbayCardConditions.ToConditionDescriptor(listing.Condition)],
        Availability: new InventoryAvailability(new ShipToLocationAvailability(listing.Quantity)));

    public OfferRequest MapOffer(CardListing listing) => new(
        Sku: listing.Sku,
        MarketplaceId: _ebay.MarketplaceId,
        Format: FixedPriceFormat,
        ListingDuration: GoodTilCancelledDuration,
        AvailableQuantity: listing.Quantity,
        CategoryId: listing.CategoryId ?? _defaults.CategoryId,
        ListingPolicies: new ListingPolicies(_defaults.FulfillmentPolicyId, _defaults.PaymentPolicyId, _defaults.ReturnPolicyId),
        PricingSummary: new PricingSummary(new Amount(listing.Price.ToString("0.00", CultureInfo.InvariantCulture), _ebay.Currency)),
        MerchantLocationKey: _defaults.MerchantLocationKey,
        StoreCategoryNames: listing.StoreCategory is null ? null : [ToStoreCategoryPath(listing.StoreCategory)]);

    private static Dictionary<string, IReadOnlyList<string>> BuildAspects(CardListing listing)
    {
        var aspects = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        AddAspect(aspects, CardAspectNames.Game, listing.Game);
        AddAspect(aspects, CardAspectNames.CardName, listing.CardName);
        AddAspect(aspects, CardAspectNames.Set, listing.SetName);
        AddAspect(aspects, CardAspectNames.CardNumber, listing.CardNumber);
        AddAspect(aspects, CardAspectNames.Rarity, listing.Rarity);
        AddAspect(aspects, CardAspectNames.Language, listing.Language);

        foreach (var (name, value) in listing.AdditionalAspects)
        {
            aspects[name] = SplitAspectValues(value);
        }

        return aspects;
    }

    private static void AddAspect(Dictionary<string, IReadOnlyList<string>> aspects, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            aspects[name] = [value];
        }
    }

    private static string[] SplitAspectValues(string value) =>
        value.Split(ListingColumns.ImagePathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string ToStoreCategoryPath(string storeCategory) =>
        storeCategory.StartsWith(StoreCategoryPathSeparator, StringComparison.Ordinal)
            ? storeCategory
            : StoreCategoryPathSeparator + storeCategory;
}
