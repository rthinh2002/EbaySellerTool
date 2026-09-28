using System.Text.RegularExpressions;

namespace EbaySellerTool.Core.Cards;

public static partial class CardNumbers
{
    /// <summary>
    /// Converts the printed "set · number/set size" form (Riftbound prints "OGN · 197/298", or "SFD · R04b" for
    /// special cards) into the "OGN-197" form used for listings. Any other format is returned unchanged apart from trimming.
    /// </summary>
    public static string Normalize(string cardNumber)
    {
        var trimmed = cardNumber.Trim();
        var match = PrintedCardNumber().Match(trimmed);

        return match.Success
            ? $"{match.Groups["set"].Value.ToUpperInvariant()}-{match.Groups["number"].Value}"
            : trimmed;
    }

    // Separator: a middle dot, bullet, full stop or hyphen (with optional spaces), or just spaces.
    // Number: printed zeros kept, at most one leading letter (special cards like "R04b") and one variant letter,
    // so Yu-Gi-Oh codes such as "LOB-EN001" don't match. The "/set size" part is optional.
    [GeneratedRegex(@"^(?<set>[A-Za-z0-9]{2,6})(?:\s*[·•.\-]\s*|\s+)(?<number>[A-Za-z]?\d{1,4}[A-Za-z]?)(?:\s*/\s*\d{1,4})?$")]
    private static partial Regex PrintedCardNumber();
}
