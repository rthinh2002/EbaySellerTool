namespace EbaySellerTool.Core.Ebay.Inventory.Models;

public sealed record BulkRequest<TRequest>(IReadOnlyList<TRequest> Requests);
