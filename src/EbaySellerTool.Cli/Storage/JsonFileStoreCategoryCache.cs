using System.Text.Json;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay.Stores;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Cli.Storage;

internal sealed class JsonFileStoreCategoryCache(IOptions<EbayOptions> ebayOptions) : IStoreCategoryCache
{
    private readonly string _filePath =
        AppDataPaths.GetFilePath($"store-categories.{ebayOptions.Value.Environment.ToString().ToLowerInvariant()}.json");

    public IReadOnlyList<StoreCategory>? Load() =>
        File.Exists(_filePath) ? JsonSerializer.Deserialize<List<StoreCategory>>(File.ReadAllText(_filePath)) : null;

    public void Save(IReadOnlyList<StoreCategory> categories) =>
        File.WriteAllText(_filePath, JsonSerializer.Serialize(categories));
}
