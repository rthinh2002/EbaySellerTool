using EbaySellerTool.Core.Ebay.Stores;

namespace EbaySellerTool.Tests.TestSupport;

internal sealed class InMemoryStoreCategoryCache(IReadOnlyList<StoreCategory>? categories = null) : IStoreCategoryCache
{
    private IReadOnlyList<StoreCategory>? _categories = categories;

    public static InMemoryStoreCategoryCache WithPaths(params string[] paths) =>
        new([.. paths.Select((path, index) => new StoreCategory($"{index}", path.TrimStart('/'), path, 1))]);

    public IReadOnlyList<StoreCategory>? Load() => _categories;

    public void Save(IReadOnlyList<StoreCategory> categories) => _categories = categories;
}
