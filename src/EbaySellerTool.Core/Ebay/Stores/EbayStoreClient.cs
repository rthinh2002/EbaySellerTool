using System.Net;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Http;

namespace EbaySellerTool.Core.Ebay.Stores;

public sealed class EbayStoreClient(EbayRestClient restClient) : IEbayStoreClient
{
    private const string PathSeparator = "/";

    public async Task<IReadOnlyList<StoreCategory>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var response = await restClient.GetAsync<CategoriesResponse>(restClient.ApiUrl("sell/stores/v1/store/categories"), cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new EbayNotSignedInException("Your eBay sign-in doesn't include access to your Store. Run 'ebaytool auth' again.");
        }

        var categories = response.EnsureSuccess("Reading store categories").Value?.StoreCategories ?? [];
        return [.. Flatten(categories, parentPath: string.Empty)];
    }

    private static IEnumerable<StoreCategory> Flatten(IEnumerable<CategoryNode> categories, string parentPath)
    {
        foreach (var category in categories.OrderBy(category => category.Order))
        {
            var path = parentPath + PathSeparator + category.CategoryName;
            yield return new StoreCategory(category.CategoryId, category.CategoryName, path, category.Level);

            foreach (var child in Flatten(category.ChildrenCategories ?? [], path))
            {
                yield return child;
            }
        }
    }

    private sealed record CategoriesResponse(IReadOnlyList<CategoryNode>? StoreCategories);

    private sealed record CategoryNode(string CategoryId, string CategoryName, int Level, int Order, IReadOnlyList<CategoryNode>? ChildrenCategories);
}
