using EbaySellerTool.Core.Cards;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Cards;

public class SkuGeneratorTests
{
    [Fact]
    public void Generate_WithCardDetails_CombinesNumberRarityAndCondition()
    {
        var listing = TestListings.Valid() with { CardNumber = "LOB-EN001", Rarity = "Ultra Rare", Condition = CardCondition.LightlyPlayed };

        Assert.Equal("LOB-EN001-ULTRA-RARE-LP", SkuGenerator.Generate(listing));
    }

    [Fact]
    public void Generate_WithoutRarity_SkipsRarity()
    {
        var listing = TestListings.Valid() with { CardNumber = "OGN-042", Rarity = null };

        Assert.Equal("OGN-042-NM", SkuGenerator.Generate(listing));
    }

    [Fact]
    public void Generate_WithoutCardNumber_ReturnsNull()
    {
        var listing = TestListings.Valid() with { CardNumber = null };

        Assert.Null(SkuGenerator.Generate(listing));
    }

    [Fact]
    public void Generate_LongValues_TruncatesToMaxSkuLength()
    {
        var listing = TestListings.Valid() with { Rarity = new string('R', 80) };

        Assert.Equal(ListingLimits.MaxSkuLength, SkuGenerator.Generate(listing)!.Length);
    }
}
