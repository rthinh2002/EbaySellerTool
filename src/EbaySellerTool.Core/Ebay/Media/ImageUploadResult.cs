using System.Diagnostics.CodeAnalysis;

namespace EbaySellerTool.Core.Ebay.Media;

/// <param name="ExpiresAt">When eBay deletes the image if no listing uses it; null when unknown.</param>
public sealed record ImageUploadResult(string? ImageUrl, string? Error, DateTimeOffset? ExpiresAt = null)
{
    [MemberNotNullWhen(true, nameof(ImageUrl))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => ImageUrl is not null;

    public static ImageUploadResult Success(string imageUrl, DateTimeOffset? expiresAt = null) => new(imageUrl, null, expiresAt);

    public static ImageUploadResult Failure(string error) => new(null, error);
}
