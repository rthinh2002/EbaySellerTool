namespace EbaySellerTool.Core.Excel;

public sealed record ListingSheet(IReadOnlyList<string> Headers, IReadOnlyList<ListingRow> Rows)
{
    public static ListingSheet Empty { get; } = new([], []);

    public bool HasColumn(ColumnDefinition column) =>
        Headers.Any(header => ListingColumns.NormalizeHeader(header) == column.Key);
}
