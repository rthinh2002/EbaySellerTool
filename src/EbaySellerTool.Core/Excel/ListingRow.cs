namespace EbaySellerTool.Core.Excel;

/// <param name="Values">Cell text keyed by normalized column header.</param>
/// <param name="Aspects">Values from "Aspect:Name" columns, keyed by aspect name.</param>
public sealed record ListingRow(
    int RowNumber,
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyDictionary<string, string> Aspects)
{
    public string? GetValue(ColumnDefinition column) =>
        Values.TryGetValue(column.Key, out var value) && value.Length > 0 ? value : null;
}
