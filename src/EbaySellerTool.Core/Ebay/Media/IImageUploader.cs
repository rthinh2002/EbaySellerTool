namespace EbaySellerTool.Core.Ebay.Media;

public interface IImageUploader
{
    Task<ImageUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken);
}
