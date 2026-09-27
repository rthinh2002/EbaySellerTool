using System.Diagnostics.CodeAnalysis;

namespace EbaySellerTool.Core.Recognition;

public sealed record CardRecognitionResult(RecognizedCard? Card, string? Error)
{
    [MemberNotNullWhen(true, nameof(Card))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Card is not null;

    public static CardRecognitionResult Success(RecognizedCard card) => new(card, null);

    public static CardRecognitionResult Failure(string error) => new(null, error);
}
