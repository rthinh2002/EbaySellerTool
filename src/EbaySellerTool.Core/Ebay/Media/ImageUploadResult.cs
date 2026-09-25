using System.Diagnostics.CodeAnalysis;

namespace EbaySellerTool.Core.Ebay.Media;

public sealed record ImageUploadResult(string? ImageUrl, string? Error)
{
    [MemberNotNullWhen(true, nameof(ImageUrl))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => ImageUrl is not null;

    public static ImageUploadResult Success(string imageUrl) => new(imageUrl, null);

    public static ImageUploadResult Failure(string error) => new(null, error);
}
