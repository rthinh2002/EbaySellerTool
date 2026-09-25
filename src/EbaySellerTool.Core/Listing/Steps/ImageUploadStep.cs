using EbaySellerTool.Core.Ebay.Media;

namespace EbaySellerTool.Core.Listing.Steps;

public sealed class ImageUploadStep(IImageUploader imageUploader) : IListingStep
{
    public async Task ExecuteAsync(IReadOnlyList<ListingJob> jobs, CancellationToken cancellationToken)
    {
        foreach (var job in jobs)
        {
            await UploadImagesAsync(job, cancellationToken);
        }
    }

    private async Task UploadImagesAsync(ListingJob job, CancellationToken cancellationToken)
    {
        var imageUrls = new List<string>(job.Listing.ImagePaths.Count);

        foreach (var imagePath in job.Listing.ImagePaths)
        {
            var result = await imageUploader.UploadAsync(imagePath, cancellationToken);

            if (!result.IsSuccess)
            {
                job.Fail($"Image upload failed for {Path.GetFileName(imagePath)}: {result.Error}");
                return;
            }

            imageUrls.Add(result.ImageUrl);
        }

        job.SetImageUrls(imageUrls);
    }
}
