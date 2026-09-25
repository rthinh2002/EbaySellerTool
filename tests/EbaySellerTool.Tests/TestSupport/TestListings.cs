using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Tests.TestSupport;

internal static class TestListings
{
    public static CardListing Valid(int rowNumber = 2) => new()
    {
        RowNumber = rowNumber,
        Sku = "LOB-EN001-UR-NM",
        Title = "Blue-Eyes White Dragon LOB-EN001 Ultra Rare",
        Game = "Yu-Gi-Oh! TCG",
        CardName = "Blue-Eyes White Dragon",
        SetName = "Legend of Blue Eyes White Dragon",
        CardNumber = "LOB-EN001",
        Rarity = "Ultra Rare",
        Language = "English",
        Condition = CardCondition.NearMintOrBetter,
        Quantity = 1,
        Price = 49.95m,
        ImagePaths = []
    };
}
