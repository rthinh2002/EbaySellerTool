using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Tests.Cards;

public class CardNumbersTests
{
    [Theory]
    [InlineData("OGN · 197/298", "OGN-197")]
    [InlineData("OGN • 006/298", "OGN-006")]
    [InlineData("VEN . 153/166", "VEN-153")]
    [InlineData("OGN 006/298", "OGN-006")]
    [InlineData("ogn-066a/298", "OGN-066a")]
    [InlineData("  OGN·197 / 298 ", "OGN-197")]
    [InlineData("SFD - R04b", "SFD-R04b")]
    [InlineData("VEN · R04a", "VEN-R04a")]
    public void Normalize_PrintedRiftboundNumber_ReturnsSetDashNumber(string printed, string expected)
    {
        Assert.Equal(expected, CardNumbers.Normalize(printed));
    }

    [Theory]
    [InlineData("LOB-EN001")]
    [InlineData("OGN-197")]
    [InlineData("197/298")]
    [InlineData("Promo")]
    public void Normalize_OtherFormats_ReturnsValueUnchanged(string cardNumber)
    {
        Assert.Equal(cardNumber, CardNumbers.Normalize($" {cardNumber} "));
    }
}
