using EbaySellerTool.Core.Descriptions;
using EbaySellerTool.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Tests.Descriptions;

public class ListingDescriptionBuilderTests
{
    [Fact]
    public void Build_DefaultTemplate_ListsOnlyFilledInDetails()
    {
        var listing = TestListings.Valid() with { Rarity = null };

        var html = CreateBuilder().Build(listing);

        Assert.Contains("<li><strong>Card Number:</strong> LOB-EN001</li>", html);
        Assert.Contains("<li><strong>Condition:</strong> Near Mint or Better</li>", html);
        Assert.DoesNotContain("Rarity", html);
        Assert.DoesNotContain("{{", html);
    }

    [Fact]
    public void Build_HtmlEncodesValuesAndKeepsLineBreaks()
    {
        var listing = TestListings.Valid() with { CardName = "Ash & <Pikachu>", Description = "Line one\nLine two" };

        var html = CreateBuilder().Build(listing);

        Assert.Contains("Ash &amp; &lt;Pikachu&gt;", html);
        Assert.Contains("<p>Line one<br>Line two</p>", html);
    }

    [Fact]
    public void Build_CustomTemplate_ReplacesPlaceholders()
    {
        using var directory = new TemporaryDirectory();
        var templatePath = directory.GetFilePath("template.html");
        File.WriteAllText(templatePath, "<h1>{{CardName}}</h1><span>{{condition}}</span>");

        var html = CreateBuilder(templatePath).Build(TestListings.Valid());

        Assert.Equal("<h1>Blue-Eyes White Dragon</h1><span>Near Mint or Better</span>", html);
    }

    private static ListingDescriptionBuilder CreateBuilder(string templatePath = "") =>
        new(Options.Create(TestSettings.ListingDefaults(templatePath)));
}
