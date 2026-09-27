using System.Net;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Stores;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Ebay;

public class EbayStoreClientTests
{
    private readonly EbayClientTestContext _context = new();

    [Fact]
    public async Task GetCategoriesAsync_NestedCategories_FlattensWithPathsInDisplayOrder()
    {
        _context.Http.RespondWith(HttpStatusCode.OK, """
            {"storeCategories":[
              {"categoryId":"20","categoryName":"Yu-Gi-Oh","level":1,"order":2,"childrenCategories":[
                {"categoryId":"21","categoryName":"Singles","level":2,"order":1}]},
              {"categoryId":"10","categoryName":"Riftbound","level":1,"order":1,"childrenCategories":[
                {"categoryId":"12","categoryName":"Sealed","level":2,"order":2},
                {"categoryId":"11","categoryName":"Singles","level":2,"order":1}]}
            ]}
            """);

        var categories = await new EbayStoreClient(_context.RestClient).GetCategoriesAsync(CancellationToken.None);

        Assert.Equal(
            ["/Riftbound", "/Riftbound/Singles", "/Riftbound/Sealed", "/Yu-Gi-Oh", "/Yu-Gi-Oh/Singles"],
            categories.Select(category => category.Path));
        Assert.EndsWith("sell/stores/v1/store/categories", _context.Http.Requests.Single().Request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetCategoriesAsync_SignInWithoutStoreScope_AsksToSignInAgain()
    {
        _context.Http.RespondWith(HttpStatusCode.Forbidden, """{"errors":[{"errorId":1100,"message":"Access denied"}]}""");

        await Assert.ThrowsAsync<EbayNotSignedInException>(
            () => new EbayStoreClient(_context.RestClient).GetCategoriesAsync(CancellationToken.None));
    }
}
