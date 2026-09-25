using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Ebay.Inventory;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Ebay;

public class ListingRequestMapperTests
{
    private readonly ListingRequestMapper _mapper = TestSettings.CreateRequestMapper();

    [Fact]
    public void MapInventoryItem_RawCard_UsesUngradedConditionWithCardConditionDescriptor()
    {
        var listing = TestListings.Valid() with { Condition = CardCondition.LightlyPlayed };

        var item = _mapper.MapInventoryItem(listing, []);

        Assert.Equal(EbayCardConditions.UngradedCondition, item.Condition);
        var descriptor = Assert.Single(item.ConditionDescriptors);
        Assert.Equal(EbayCardConditions.CardConditionDescriptorName, descriptor.Name);
        Assert.Equal(["400011"], descriptor.Values);
    }

    [Fact]
    public void MapInventoryItem_CopiesSkuTitleQuantityLocaleAndImages()
    {
        var listing = TestListings.Valid() with { Quantity = 3 };
        string[] imageUrls = ["https://i.ebayimg.test/front.jpg"];

        var item = _mapper.MapInventoryItem(listing, imageUrls);

        Assert.Equal(listing.Sku, item.Sku);
        Assert.Equal("en_AU", item.Locale);
        Assert.Equal(listing.Title, item.Product.Title);
        Assert.Equal(imageUrls, item.Product.ImageUrls);
        Assert.Equal(3, item.Availability.ShipToLocationAvailability.Quantity);
        Assert.Contains(listing.CardName, item.Product.Description);
    }

    [Fact]
    public void MapInventoryItem_BuildsAspectsFromCardDetails()
    {
        var listing = TestListings.Valid() with { Rarity = null };

        var aspects = _mapper.MapInventoryItem(listing, []).Product.Aspects;

        Assert.Equal(["Yu-Gi-Oh! TCG"], aspects[CardAspectNames.Game]);
        Assert.Equal(["Blue-Eyes White Dragon"], aspects[CardAspectNames.CardName]);
        Assert.Equal(["Legend of Blue Eyes White Dragon"], aspects[CardAspectNames.Set]);
        Assert.Equal(["LOB-EN001"], aspects[CardAspectNames.CardNumber]);
        Assert.Equal(["English"], aspects[CardAspectNames.Language]);
        Assert.False(aspects.ContainsKey(CardAspectNames.Rarity));
    }

    [Fact]
    public void MapInventoryItem_AdditionalAspects_OverrideAndSplitMultipleValues()
    {
        var listing = TestListings.Valid() with
        {
            AdditionalAspects = new Dictionary<string, string>
            {
                ["Game"] = "Yu-Gi-Oh!",
                ["Features"] = "1st Edition | Holo"
            }
        };

        var aspects = _mapper.MapInventoryItem(listing, []).Product.Aspects;

        Assert.Equal(["Yu-Gi-Oh!"], aspects[CardAspectNames.Game]);
        Assert.Equal(["1st Edition", "Holo"], aspects["Features"]);
    }

    [Fact]
    public void MapOffer_UsesConfiguredDefaultsAndFormatsPrice()
    {
        var listing = TestListings.Valid() with { Price = 5m, Quantity = 2 };

        var offer = _mapper.MapOffer(listing);

        Assert.Equal(listing.Sku, offer.Sku);
        Assert.Equal("EBAY_AU", offer.MarketplaceId);
        Assert.Equal("FIXED_PRICE", offer.Format);
        Assert.Equal("GTC", offer.ListingDuration);
        Assert.Equal(2, offer.AvailableQuantity);
        Assert.Equal("183454", offer.CategoryId);
        Assert.Equal("home", offer.MerchantLocationKey);
        Assert.Equal("5.00", offer.PricingSummary.Price.Value);
        Assert.Equal("AUD", offer.PricingSummary.Price.Currency);
        Assert.Equal("fulfillment-1", offer.ListingPolicies.FulfillmentPolicyId);
        Assert.Equal("payment-1", offer.ListingPolicies.PaymentPolicyId);
        Assert.Equal("return-1", offer.ListingPolicies.ReturnPolicyId);
        Assert.Null(offer.StoreCategoryNames);
    }

    [Fact]
    public void MapOffer_RowCategoryId_OverridesDefault()
    {
        var offer = _mapper.MapOffer(TestListings.Valid() with { CategoryId = "999" });

        Assert.Equal("999", offer.CategoryId);
    }

    [Theory]
    [InlineData("/Yu-Gi-Oh/Singles")]
    [InlineData("Yu-Gi-Oh/Singles")]
    public void MapOffer_StoreCategory_IsSentAsPathWithLeadingSlash(string storeCategory)
    {
        var offer = _mapper.MapOffer(TestListings.Valid() with { StoreCategory = storeCategory });

        Assert.Equal(["/Yu-Gi-Oh/Singles"], offer.StoreCategoryNames);
    }
}
