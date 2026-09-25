using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Ebay.Inventory.Models;

namespace EbaySellerTool.Core.Ebay.Inventory;

/// <summary>
/// Trading card categories list raw cards with the "Ungraded" condition (ID 4000, which the Inventory API calls
/// USED_VERY_GOOD) plus a "Card Condition" descriptor (40001) holding the actual card grade.
/// </summary>
public static class EbayCardConditions
{
    public const string UngradedCondition = "USED_VERY_GOOD";
    public const string CardConditionDescriptorName = "40001";

    private static readonly IReadOnlyDictionary<CardCondition, string> DescriptorValueIds = new Dictionary<CardCondition, string>
    {
        [CardCondition.NearMintOrBetter] = "400010",
        [CardCondition.LightlyPlayed] = "400011",
        [CardCondition.ModeratelyPlayed] = "400012",
        [CardCondition.HeavilyPlayed] = "400013"
    };

    public static ConditionDescriptor ToConditionDescriptor(CardCondition condition) =>
        new(CardConditionDescriptorName, [DescriptorValueIds[condition]]);
}
