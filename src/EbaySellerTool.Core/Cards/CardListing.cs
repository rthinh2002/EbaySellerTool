namespace EbaySellerTool.Core.Cards;

public sealed record CardListing
{
    public required int RowNumber { get; init; }
    public required string Sku { get; init; }
    public required string Title { get; init; }
    public required string Game { get; init; }
    public required string CardName { get; init; }
    public string? SetName { get; init; }
    public string? CardNumber { get; init; }
    public string? Rarity { get; init; }
    public string? Language { get; init; }
    public required CardCondition Condition { get; init; }
    public required int Quantity { get; init; }
    public required decimal Price { get; init; }
    public required IReadOnlyList<string> ImagePaths { get; init; }
    public string? StoreCategory { get; init; }
    public string? CategoryId { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, string> AdditionalAspects { get; init; } = new Dictionary<string, string>();
}
