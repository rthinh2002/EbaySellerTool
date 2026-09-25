using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Core.Validation.Rules;

public sealed class ImagesRule : IListingRule
{
    public IEnumerable<ValidationError> Validate(CardListing listing)
    {
        if (listing.ImagePaths.Count == 0)
        {
            yield return Error(listing, "At least one image is required.");
            yield break;
        }

        if (listing.ImagePaths.Count > ListingLimits.MaxImagesPerListing)
        {
            yield return Error(listing, $"{listing.ImagePaths.Count} images given; the maximum is {ListingLimits.MaxImagesPerListing}.");
        }

        foreach (var imagePath in listing.ImagePaths)
        {
            if (!ListingLimits.SupportedImageExtensions.Contains(Path.GetExtension(imagePath)))
            {
                yield return Error(listing, $"Unsupported image type: {imagePath}");
            }
            else if (!File.Exists(imagePath))
            {
                yield return Error(listing, $"Image not found: {imagePath}");
            }
        }
    }

    private static ValidationError Error(CardListing listing, string message) =>
        new(listing.RowNumber, ListingColumns.Images.Header, message);
}
