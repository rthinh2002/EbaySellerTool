using System.Net.Http.Headers;
using EbaySellerTool.Core.Ebay.Http;

namespace EbaySellerTool.Core.Ebay.Media;

/// <summary>Uploads images to eBay Picture Services through the Media API. Not available in the Sandbox.</summary>
public sealed class EbayMediaImageUploader(EbayRestClient restClient) : IImageUploader
{
    private const string CreateImagePath = "commerce/media/v1_beta/image/create_image_from_file";
    private const string ImageFormField = "image";

    private static readonly IReadOnlyDictionary<string, string> MediaTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".tif"] = "image/tiff",
        [".tiff"] = "image/tiff",
        [".webp"] = "image/webp",
        [".heic"] = "image/heic",
        [".avif"] = "image/avif"
    };

    public async Task<ImageUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, restClient.MediaUrl(CreateImagePath))
        {
            Content = await CreateFormAsync(filePath, cancellationToken)
        };

        var response = await restClient.SendAsync<ImageDetails>(request, cancellationToken);

        if (!response.IsSuccess)
        {
            return ImageUploadResult.Failure(DescribeFailure(response));
        }

        // The image URL may come back in the body; otherwise it is fetched from the Location header's getImage URI.
        var details = response.Value?.ImageUrl is not null ? response.Value : await GetImageAsync(response.Location, cancellationToken);

        return details?.ImageUrl is null
            ? ImageUploadResult.Failure("eBay didn't return an image URL.")
            : ImageUploadResult.Success(details.ImageUrl, details.ExpirationDate);
    }

    private static async Task<MultipartFormDataContent> CreateFormAsync(string filePath, CancellationToken cancellationToken)
    {
        var imageContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath, cancellationToken));
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(MediaTypes.GetValueOrDefault(Path.GetExtension(filePath), "application/octet-stream"));

        return new MultipartFormDataContent { { imageContent, ImageFormField, Path.GetFileName(filePath) } };
    }

    private async Task<ImageDetails?> GetImageAsync(Uri? imageUri, CancellationToken cancellationToken)
    {
        if (imageUri is null)
        {
            return null;
        }

        var response = await restClient.GetAsync<ImageDetails>(imageUri, cancellationToken);
        return response.IsSuccess ? response.Value : null;
    }

    private static string DescribeFailure<T>(EbayResponse<T> response) =>
        response.Errors.Count == 0
            ? $"HTTP {(int)response.StatusCode}"
            : string.Join("; ", response.Errors.Select(error => error.ToDisplayString()));

    private sealed record ImageDetails(string? ImageUrl, DateTimeOffset? ExpirationDate);
}
