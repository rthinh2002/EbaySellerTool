using System.Security.Cryptography;

namespace EbaySellerTool.Core.Ebay.Media;

/// <summary>
/// Skips uploading an image whose content was already uploaded, so re-running a sheet doesn't upload
/// every photo again. Entries close to eBay's expiry date are uploaded afresh.
/// </summary>
public sealed class CachingImageUploader(IImageUploader innerUploader, IImageUrlCache cache, TimeProvider timeProvider) : IImageUploader
{
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromDays(1);

    public async Task<ImageUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken)
    {
        var contentHash = await HashFileAsync(filePath, cancellationToken);
        var cachedImage = await cache.FindAsync(contentHash, cancellationToken);

        if (cachedImage is not null && !IsAboutToExpire(cachedImage))
        {
            return ImageUploadResult.Success(cachedImage.ImageUrl, cachedImage.ExpiresAt);
        }

        var result = await innerUploader.UploadAsync(filePath, cancellationToken);

        if (result.IsSuccess)
        {
            await cache.SaveAsync(contentHash, new CachedImage(result.ImageUrl, result.ExpiresAt), cancellationToken);
        }

        return result;
    }

    private bool IsAboutToExpire(CachedImage image) =>
        image.ExpiresAt is { } expiresAt && expiresAt - ExpiryMargin <= timeProvider.GetUtcNow();

    private static async Task<string> HashFileAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
    }
}
