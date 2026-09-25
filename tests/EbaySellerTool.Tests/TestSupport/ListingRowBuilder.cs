using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Tests.TestSupport;

internal sealed class ListingRowBuilder
{
    private readonly Dictionary<string, string> _values = new();
    private readonly Dictionary<string, string> _aspects = new(StringComparer.OrdinalIgnoreCase);

    public static ListingRowBuilder ValidRow() => new ListingRowBuilder()
        .With(ListingColumns.Game, "Yu-Gi-Oh! TCG")
        .With(ListingColumns.CardName, "Blue-Eyes White Dragon")
        .With(ListingColumns.CardNumber, "LOB-EN001")
        .With(ListingColumns.Rarity, "Ultra Rare")
        .With(ListingColumns.CardCondition, "Near Mint or Better")
        .With(ListingColumns.Price, "49.95")
        .With(ListingColumns.Images, "front.jpg");

    public ListingRowBuilder With(ColumnDefinition column, string value)
    {
        _values[column.Key] = value;
        return this;
    }

    public ListingRowBuilder Without(ColumnDefinition column)
    {
        _values.Remove(column.Key);
        return this;
    }

    public ListingRowBuilder WithAspect(string name, string value)
    {
        _aspects[name] = value;
        return this;
    }

    public ListingRow Build(int rowNumber = 2) => new(rowNumber, _values, _aspects);
}
