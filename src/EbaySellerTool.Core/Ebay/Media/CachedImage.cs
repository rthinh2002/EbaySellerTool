namespace EbaySellerTool.Core.Ebay.Media;

public sealed record CachedImage(string ImageUrl, DateTimeOffset? ExpiresAt);
