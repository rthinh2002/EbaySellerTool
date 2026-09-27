using EbaySellerTool.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Core.Ebay.Media;

/// <summary>
/// The Media API doesn't support the Sandbox, so Sandbox listings use one public placeholder image
/// (<see cref="EbayOptions.SandboxPlaceholderImageUrl"/>) for every local file.
/// </summary>
public sealed class SandboxPlaceholderImageUploader(IOptions<EbayOptions> ebayOptions) : IImageUploader
{
    public Task<ImageUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken) =>
        Task.FromResult(ImageUploadResult.Success(ebayOptions.Value.SandboxPlaceholderImageUrl));
}
