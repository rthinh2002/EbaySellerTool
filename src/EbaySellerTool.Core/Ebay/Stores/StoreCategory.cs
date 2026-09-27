namespace EbaySellerTool.Core.Ebay.Stores;

/// <param name="Path">The value offers use in <c>storeCategoryNames</c>, e.g. "/Riftbound/Singles".</param>
public sealed record StoreCategory(string Id, string Name, string Path, int Level);
