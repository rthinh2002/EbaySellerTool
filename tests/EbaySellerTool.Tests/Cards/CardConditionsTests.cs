using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Tests.Cards;

public class CardConditionsTests
{
    [Theory]
    [InlineData("Near Mint or Better", CardCondition.NearMintOrBetter)]
    [InlineData("nm", CardCondition.NearMintOrBetter)]
    [InlineData(" LP ", CardCondition.LightlyPlayed)]
    [InlineData("Lightly Played (Excellent)", CardCondition.LightlyPlayed)]
    [InlineData("Very Good", CardCondition.ModeratelyPlayed)]
    [InlineData("HP", CardCondition.HeavilyPlayed)]
    public void TryParse_KnownText_ReturnsCondition(string text, CardCondition expected)
    {
        var parsed = CardConditions.TryParse(text, out var condition);

        Assert.True(parsed);
        Assert.Equal(expected, condition);
    }

    [Theory]
    [InlineData("Damaged")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_UnknownText_ReturnsFalse(string? text)
    {
        Assert.False(CardConditions.TryParse(text, out _));
    }
}
