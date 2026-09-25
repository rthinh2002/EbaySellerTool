namespace EbaySellerTool.Core.Cards;

public static class CardConditions
{
    private static readonly IReadOnlyDictionary<CardCondition, string> DisplayNames = new Dictionary<CardCondition, string>
    {
        [CardCondition.NearMintOrBetter] = "Near Mint or Better",
        [CardCondition.LightlyPlayed] = "Lightly Played (Excellent)",
        [CardCondition.ModeratelyPlayed] = "Moderately Played (Very Good)",
        [CardCondition.HeavilyPlayed] = "Heavily Played (Poor)"
    };

    private static readonly IReadOnlyDictionary<CardCondition, string> Codes = new Dictionary<CardCondition, string>
    {
        [CardCondition.NearMintOrBetter] = "NM",
        [CardCondition.LightlyPlayed] = "LP",
        [CardCondition.ModeratelyPlayed] = "MP",
        [CardCondition.HeavilyPlayed] = "HP"
    };

    private static readonly IReadOnlyDictionary<string, CardCondition> Aliases = BuildAliases();

    public static IReadOnlyList<string> AllDisplayNames { get; } = [.. DisplayNames.Values];

    public static string ToDisplayName(this CardCondition condition) => DisplayNames[condition];

    public static string ToCode(this CardCondition condition) => Codes[condition];

    public static bool TryParse(string? text, out CardCondition condition)
    {
        condition = default;
        return text is not null && Aliases.TryGetValue(text.Trim(), out condition);
    }

    private static Dictionary<string, CardCondition> BuildAliases()
    {
        var aliases = new Dictionary<string, CardCondition>(StringComparer.OrdinalIgnoreCase)
        {
            ["Near Mint"] = CardCondition.NearMintOrBetter,
            ["Mint"] = CardCondition.NearMintOrBetter,
            ["Lightly Played"] = CardCondition.LightlyPlayed,
            ["Excellent"] = CardCondition.LightlyPlayed,
            ["Moderately Played"] = CardCondition.ModeratelyPlayed,
            ["Very Good"] = CardCondition.ModeratelyPlayed,
            ["Heavily Played"] = CardCondition.HeavilyPlayed,
            ["Poor"] = CardCondition.HeavilyPlayed
        };

        foreach (var condition in Enum.GetValues<CardCondition>())
        {
            aliases[condition.ToDisplayName()] = condition;
            aliases[condition.ToCode()] = condition;
        }

        return aliases;
    }
}
