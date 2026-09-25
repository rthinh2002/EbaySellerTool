using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Import;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Import;

public class CardListingParserTests
{
    private static readonly string BaseDirectory = Path.Combine(Path.GetTempPath(), "cards");
    private readonly CardListingParser _parser = new();

    [Fact]
    public void Parse_ValidRow_ReturnsListing()
    {
        var row = ListingRowBuilder.ValidRow().WithAspect("Edition", "1st Edition").Build(rowNumber: 5);

        var result = _parser.Parse(row, BaseDirectory);

        Assert.Empty(result.Errors);
        var listing = result.Listing!;
        Assert.Equal(5, listing.RowNumber);
        Assert.Equal(CardCondition.NearMintOrBetter, listing.Condition);
        Assert.Equal(49.95m, listing.Price);
        Assert.Equal(1, listing.Quantity);
        Assert.Equal("1st Edition", listing.AdditionalAspects["Edition"]);
    }

    [Fact]
    public void Parse_MissingRequiredValues_ReturnsErrorPerColumn()
    {
        var row = ListingRowBuilder.ValidRow()
            .Without(ListingColumns.Game)
            .Without(ListingColumns.CardName)
            .Without(ListingColumns.Price)
            .Build();

        var result = _parser.Parse(row, BaseDirectory);

        Assert.Null(result.Listing);
        Assert.Equal(
            [ListingColumns.Game.Header, ListingColumns.CardName.Header, ListingColumns.Price.Header],
            result.Errors.Select(error => error.Column));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.5.0")]
    public void Parse_InvalidPrice_ReturnsPriceError(string price)
    {
        var row = ListingRowBuilder.ValidRow().With(ListingColumns.Price, price).Build();

        var result = _parser.Parse(row, BaseDirectory);

        Assert.Equal(ListingColumns.Price.Header, Assert.Single(result.Errors).Column);
    }

    [Fact]
    public void Parse_PriceWithDollarSign_ParsesPrice()
    {
        var row = ListingRowBuilder.ValidRow().With(ListingColumns.Price, "$12.50").Build();

        Assert.Equal(12.50m, _parser.Parse(row, BaseDirectory).Listing!.Price);
    }

    [Fact]
    public void Parse_UnknownCondition_ReturnsConditionError()
    {
        var row = ListingRowBuilder.ValidRow().With(ListingColumns.CardCondition, "Damaged").Build();

        var result = _parser.Parse(row, BaseDirectory);

        Assert.Equal(ListingColumns.CardCondition.Header, Assert.Single(result.Errors).Column);
    }

    [Fact]
    public void Parse_FractionalQuantity_ReturnsQuantityError()
    {
        var row = ListingRowBuilder.ValidRow().With(ListingColumns.Quantity, "1.5").Build();

        var result = _parser.Parse(row, BaseDirectory);

        Assert.Equal(ListingColumns.Quantity.Header, Assert.Single(result.Errors).Column);
    }

    [Fact]
    public void Parse_ImagePaths_SplitsAndResolvesRelativeToBaseDirectory()
    {
        var absolutePath = Path.Combine(Path.GetTempPath(), "elsewhere", "back.png");
        var row = ListingRowBuilder.ValidRow()
            .With(ListingColumns.Images, $"images\\front.jpg | \"{absolutePath}\"")
            .Build();

        var listing = _parser.Parse(row, BaseDirectory).Listing!;

        Assert.Equal([Path.Combine(BaseDirectory, "images", "front.jpg"), absolutePath], listing.ImagePaths);
    }

    [Fact]
    public void Parse_BlankSkuAndTitle_GeneratesThem()
    {
        var listing = _parser.Parse(ListingRowBuilder.ValidRow().Build(), BaseDirectory).Listing!;

        Assert.Equal("LOB-EN001-ULTRA-RARE-NM", listing.Sku);
        Assert.StartsWith("Blue-Eyes White Dragon LOB-EN001", listing.Title);
    }

    [Fact]
    public void Parse_ProvidedSkuAndTitle_KeepsThem()
    {
        var row = ListingRowBuilder.ValidRow()
            .With(ListingColumns.Sku, "MY-SKU")
            .With(ListingColumns.Title, "My title")
            .Build();

        var listing = _parser.Parse(row, BaseDirectory).Listing!;

        Assert.Equal("MY-SKU", listing.Sku);
        Assert.Equal("My title", listing.Title);
    }

    [Fact]
    public void Parse_BlankSkuWithoutCardNumber_ReturnsSkuError()
    {
        var row = ListingRowBuilder.ValidRow().Without(ListingColumns.CardNumber).Build();

        var result = _parser.Parse(row, BaseDirectory);

        Assert.Equal(ListingColumns.Sku.Header, Assert.Single(result.Errors).Column);
    }

    [Fact]
    public void Parse_SeveralProblems_ReportsAllOfThemTogether()
    {
        var row = ListingRowBuilder.ValidRow()
            .Without(ListingColumns.CardNumber)
            .With(ListingColumns.Price, "abc")
            .Build();

        var result = _parser.Parse(row, BaseDirectory);

        Assert.Equal([ListingColumns.Price.Header, ListingColumns.Sku.Header], result.Errors.Select(error => error.Column));
    }
}
