namespace EbaySellerTool.Core.Ebay;

public sealed record EbayError
{
    public int ErrorId { get; init; }
    public string? Domain { get; init; }
    public string? Category { get; init; }
    public string? Message { get; init; }
    public string? LongMessage { get; init; }
    public IReadOnlyList<EbayErrorParameter> Parameters { get; init; } = [];

    public string ToDisplayString()
    {
        var message = string.IsNullOrWhiteSpace(LongMessage) ? Message : LongMessage;
        return $"{message} [eBay {ErrorId}]";
    }
}
