using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Validation.Rules;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Validation;

public class ListingRuleTests
{
    [Fact]
    public void TitleRule_TitleTooLong_ReturnsError()
    {
        var listing = TestListings.Valid() with { Title = new string('T', ListingLimits.MaxTitleLength + 1) };

        Assert.Single(new TitleRule().Validate(listing));
    }

    [Fact]
    public void SkuRule_SkuTooLong_ReturnsError()
    {
        var listing = TestListings.Valid() with { Sku = new string('S', ListingLimits.MaxSkuLength + 1) };

        Assert.Single(new SkuRule().Validate(listing));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.999")]
    public void PriceRule_InvalidPrice_ReturnsError(string price)
    {
        var listing = TestListings.Valid() with { Price = decimal.Parse(price) };

        Assert.Single(new PriceRule().Validate(listing));
    }

    [Fact]
    public void PriceRule_ValidPrice_ReturnsNoErrors()
    {
        Assert.Empty(new PriceRule().Validate(TestListings.Valid()));
    }

    [Fact]
    public void QuantityRule_ZeroQuantity_ReturnsError()
    {
        var listing = TestListings.Valid() with { Quantity = 0 };

        Assert.Single(new QuantityRule().Validate(listing));
    }

    [Fact]
    public void CategoryIdRule_NonNumericCategory_ReturnsError()
    {
        var listing = TestListings.Valid() with { CategoryId = "cards" };

        Assert.Single(new CategoryIdRule().Validate(listing));
    }

    [Fact]
    public void ImagesRule_NoImages_ReturnsError()
    {
        Assert.Single(new ImagesRule().Validate(TestListings.Valid()));
    }

    [Fact]
    public void ImagesRule_ExistingSupportedImages_ReturnsNoErrors()
    {
        using var directory = new TemporaryDirectory();
        var listing = TestListings.Valid() with { ImagePaths = [directory.CreateFile("front.jpg"), directory.CreateFile("back.PNG")] };

        Assert.Empty(new ImagesRule().Validate(listing));
    }

    [Fact]
    public void ImagesRule_MissingOrUnsupportedImages_ReturnsErrorPerImage()
    {
        using var directory = new TemporaryDirectory();
        var listing = TestListings.Valid() with
        {
            ImagePaths = [directory.GetFilePath("missing.jpg"), directory.CreateFile("notes.txt")]
        };

        var errors = new ImagesRule().Validate(listing).ToList();

        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, error => error.Message.StartsWith("Image not found"));
        Assert.Contains(errors, error => error.Message.StartsWith("Unsupported image type"));
    }

    [Fact]
    public void ImagesRule_TooManyImages_ReturnsError()
    {
        using var directory = new TemporaryDirectory();
        var imagePaths = Enumerable.Range(1, ListingLimits.MaxImagesPerListing + 1)
            .Select(index => directory.CreateFile($"{index}.jpg"))
            .ToList();
        var listing = TestListings.Valid() with { ImagePaths = imagePaths };

        Assert.Single(new ImagesRule().Validate(listing));
    }

    [Fact]
    public void DuplicateSkuRule_SameSkuOnTwoRows_ReturnsErrorForEachRow()
    {
        var listings = new[]
        {
            TestListings.Valid(rowNumber: 2),
            TestListings.Valid(rowNumber: 3) with { Sku = "lob-en001-ur-nm" },
            TestListings.Valid(rowNumber: 4) with { Sku = "OTHER" }
        };

        var errors = new DuplicateSkuRule().Validate(listings).ToList();

        Assert.Equal([2, 3], errors.Select(error => error.RowNumber));
    }
}
