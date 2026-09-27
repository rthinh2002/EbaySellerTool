using System.Text.Json;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay.Media;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Cli.Storage;

internal sealed class JsonFileImageUrlCache(IOptions<EbayOptions> ebayOptions) : IImageUrlCache
{
    private readonly string _filePath = AppDataPaths.GetFilePath($"image-cache.{ebayOptions.Value.Environment.ToString().ToLowerInvariant()}.json");
    private Dictionary<string, CachedImage>? _images;

    public async Task<CachedImage?> FindAsync(string contentHash, CancellationToken cancellationToken)
    {
        var images = await LoadAsync(cancellationToken);
        return images.GetValueOrDefault(contentHash);
    }

    public async Task SaveAsync(string contentHash, CachedImage image, CancellationToken cancellationToken)
    {
        var images = await LoadAsync(cancellationToken);
        images[contentHash] = image;

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, images, cancellationToken: cancellationToken);
    }

    private async Task<Dictionary<string, CachedImage>> LoadAsync(CancellationToken cancellationToken)
    {
        if (_images is not null)
        {
            return _images;
        }

        if (!File.Exists(_filePath))
        {
            return _images = [];
        }

        await using var stream = File.OpenRead(_filePath);
        return _images = await JsonSerializer.DeserializeAsync<Dictionary<string, CachedImage>>(stream, cancellationToken: cancellationToken) ?? [];
    }
}
