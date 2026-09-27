namespace EbaySellerTool.Core.Ebay.Http;

internal sealed record EbayErrorEnvelope
{
    public IReadOnlyList<EbayError> Errors { get; init; } = [];
}
