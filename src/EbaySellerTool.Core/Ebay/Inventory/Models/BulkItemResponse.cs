namespace EbaySellerTool.Core.Ebay.Inventory.Models;

public abstract record BulkItemResponse
{
    public int StatusCode { get; init; }
    public IReadOnlyList<EbayError> Errors { get; init; } = [];
    public IReadOnlyList<EbayError> Warnings { get; init; } = [];

    public bool IsSuccess => StatusCode is >= 200 and < 300 && Errors.Count == 0;
}
