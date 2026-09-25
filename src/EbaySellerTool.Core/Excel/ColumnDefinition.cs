namespace EbaySellerTool.Core.Excel;

public sealed record ColumnDefinition(string Header, string Description, string Example, bool IsRequired = false)
{
    public string Key { get; } = ListingColumns.NormalizeHeader(Header);
}
