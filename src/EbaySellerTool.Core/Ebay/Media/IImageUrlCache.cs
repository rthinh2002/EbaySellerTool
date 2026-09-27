namespace EbaySellerTool.Core.Ebay.Media;

/// <summary>Remembers the eBay URL of each uploaded image, keyed by a hash of the file's content.</summary>
public interface IImageUrlCache
{
    Task<CachedImage?> FindAsync(string contentHash, CancellationToken cancellationToken);

    Task SaveAsync(string contentHash, CachedImage image, CancellationToken cancellationToken);
}
