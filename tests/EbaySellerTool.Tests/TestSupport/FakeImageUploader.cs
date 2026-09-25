using EbaySellerTool.Core.Ebay.Media;

namespace EbaySellerTool.Tests.TestSupport;

internal sealed class FakeImageUploader : IImageUploader
{
    public HashSet<string> FailingFileNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> UploadedPaths { get; } = [];

    public Task<ImageUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);

        if (FailingFileNames.Contains(fileName))
        {
            return Task.FromResult(ImageUploadResult.Failure("upload rejected"));
        }

        UploadedPaths.Add(filePath);
        return Task.FromResult(ImageUploadResult.Success($"https://i.ebayimg.test/{fileName}"));
    }
}
