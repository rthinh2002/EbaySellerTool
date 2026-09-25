using EbaySellerTool.Core.Cards;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Cards;

public class ListingTitleBuilderTests
{
    [Fact]
    public void Build_WithAllDetails_JoinsPartsInOrder()
    {
        var listing = TestListings.Valid() with { SetName = "LOB" };

        Assert.Equal("Blue-Eyes White Dragon LOB-EN001 Ultra Rare LOB Yu-Gi-Oh! TCG NM", ListingTitleBuilder.Build(listing));
    }

    [Fact]
    public void Build_TooLong_SkipsPartsThatDoNotFit()
    {
        var listing = TestListings.Valid() with { SetName = new string('S', 60) };

        var title = ListingTitleBuilder.Build(listing);

        Assert.True(title.Length <= ListingLimits.MaxTitleLength);
        Assert.DoesNotContain("SSS", title);
        Assert.EndsWith("NM", title);
    }
}
