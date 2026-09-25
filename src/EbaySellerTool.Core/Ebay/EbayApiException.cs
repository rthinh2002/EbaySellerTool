namespace EbaySellerTool.Core.Ebay;

/// <summary>
/// A whole eBay request failed (for example an HTTP error or expired token), as opposed to
/// individual items being rejected inside a successful bulk response.
/// </summary>
public sealed class EbayApiException(string message, IReadOnlyList<EbayError>? errors = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public IReadOnlyList<EbayError> Errors { get; } = errors ?? [];
}
