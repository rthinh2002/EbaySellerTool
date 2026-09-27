using EbaySellerTool.Core.Validation.Rules;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Validation;

public class StoreCategoryRuleTests
{
    private static readonly InMemoryStoreCategoryCache KnownCategories = InMemoryStoreCategoryCache.WithPaths("/YU-GI-OH! Singles", "/Riftbound");

    [Theory]
    [InlineData("/Riftbound")]
    [InlineData("YU-GI-OH! Singles")]
    public void Validate_KnownCategory_ReturnsNoErrors(string storeCategory)
    {
        var listing = TestListings.Valid() with { StoreCategory = storeCategory };

        Assert.Empty(new StoreCategoryRule(KnownCategories).Validate(listing));
    }

    [Fact]
    public void Validate_DifferentCapitalisation_SuggestsExactName()
    {
        var listing = TestListings.Valid() with { StoreCategory = "/Yu-Gi-Oh! Singles" };

        var error = Assert.Single(new StoreCategoryRule(KnownCategories).Validate(listing));

        Assert.Equal("Store category '/Yu-Gi-Oh! Singles' should be written exactly as '/YU-GI-OH! Singles'.", error.Message);
    }

    [Fact]
    public void Validate_UnknownCategory_ListsKnownCategories()
    {
        var listing = TestListings.Valid() with { StoreCategory = "/Pokemon" };

        var error = Assert.Single(new StoreCategoryRule(KnownCategories).Validate(listing));

        Assert.Contains("/YU-GI-OH! Singles, /Riftbound", error.Message);
    }

    [Fact]
    public void Validate_CategoriesNeverFetched_SkipsCheck()
    {
        var listing = TestListings.Valid() with { StoreCategory = "/Anything" };

        Assert.Empty(new StoreCategoryRule(new InMemoryStoreCategoryCache()).Validate(listing));
    }
}
