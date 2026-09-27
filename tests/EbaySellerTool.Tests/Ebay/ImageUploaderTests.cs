using System.Net;
using EbaySellerTool.Core.Ebay.Media;
using EbaySellerTool.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace EbaySellerTool.Tests.Ebay;

public sealed class ImageUploaderTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly EbayClientTestContext _context = new();

    [Fact]
    public async Task EbayMediaImageUploader_UrlInBody_ReturnsIt()
    {
        var imagePath = WriteImage("front.jpg", [1, 2, 3]);
        _context.Http.RespondWith(HttpStatusCode.Created, """{"imageUrl":"https://i.ebayimg.com/front.jpg","expirationDate":"2026-10-27T00:00:00Z"}""");

        var result = await new EbayMediaImageUploader(_context.RestClient).UploadAsync(imagePath, CancellationToken.None);

        Assert.Equal("https://i.ebayimg.com/front.jpg", result.ImageUrl);
        Assert.Equal(new DateTimeOffset(2026, 10, 27, 0, 0, 0, TimeSpan.Zero), result.ExpiresAt);
        var request = _context.Http.Requests.Single().Request;
        Assert.Equal("https://apim.sandbox.ebay.com/commerce/media/v1_beta/image/create_image_from_file", request.RequestUri!.AbsoluteUri);
        Assert.Equal("multipart/form-data", request.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task EbayMediaImageUploader_Rejected_ReturnsFailureWithEbayMessage()
    {
        var imagePath = WriteImage("front.jpg", [1]);
        _context.Http.RespondWith(HttpStatusCode.BadRequest, """{"errors":[{"errorId":190000,"message":"The image is too small."}]}""");

        var result = await new EbayMediaImageUploader(_context.RestClient).UploadAsync(imagePath, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("too small", result.Error);
    }

    [Fact]
    public async Task CachingImageUploader_SameContentTwice_UploadsOnce()
    {
        var inner = new FakeImageUploader();
        var uploader = new CachingImageUploader(inner, new InMemoryImageUrlCache(), new FakeTimeProvider());
        var first = WriteImage("a.jpg", [7, 7, 7]);
        var copy = WriteImage("copy-of-a.jpg", [7, 7, 7]);

        await uploader.UploadAsync(first, CancellationToken.None);
        var result = await uploader.UploadAsync(copy, CancellationToken.None);

        Assert.Single(inner.UploadedPaths);
        Assert.Equal("https://i.ebayimg.test/a.jpg", result.ImageUrl);
    }

    [Fact]
    public async Task CachingImageUploader_CachedImageAboutToExpire_UploadsAgain()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero));
        var cache = new InMemoryImageUrlCache();
        var inner = new FakeImageUploader();
        var uploader = new CachingImageUploader(inner, cache, clock);
        var imagePath = WriteImage("a.jpg", [1, 2]);
        await cache.SaveAsync(await HashAsync(imagePath), new CachedImage("https://old", clock.GetUtcNow().AddHours(2)), CancellationToken.None);

        var result = await uploader.UploadAsync(imagePath, CancellationToken.None);

        Assert.Equal("https://i.ebayimg.test/a.jpg", result.ImageUrl);
    }

    public void Dispose() => _directory.Dispose();

    private string WriteImage(string fileName, byte[] content)
    {
        var path = _directory.GetFilePath(fileName);
        File.WriteAllBytes(path, content);
        return path;
    }

    private static async Task<string> HashAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream));
    }

    private sealed class InMemoryImageUrlCache : IImageUrlCache
    {
        private readonly Dictionary<string, CachedImage> _images = [];

        public Task<CachedImage?> FindAsync(string contentHash, CancellationToken cancellationToken) =>
            Task.FromResult(_images.GetValueOrDefault(contentHash));

        public Task SaveAsync(string contentHash, CachedImage image, CancellationToken cancellationToken)
        {
            _images[contentHash] = image;
            return Task.CompletedTask;
        }
    }
}
