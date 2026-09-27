using ClosedXML.Excel;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Recognition;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Excel;

public sealed class ListingSheetAppenderTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ListingSheetAppender _appender = new();
    private readonly string _sheetPath;

    public ListingSheetAppenderTests()
    {
        _sheetPath = _directory.GetFilePath(@"sheets\cards.xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(_sheetPath)!);
        new ListingTemplateWriter(new InMemoryStoreCategoryCache()).Write(_sheetPath);
    }

    [Fact]
    public void AppendCardRows_AddsRowPerImageWithPathRelativeToSheet()
    {
        var addedRows = _appender.AppendCardRows(_sheetPath, [Row("scan_card01.jpg"), Row("scan_card02.jpg")]);

        Assert.Equal(2, addedRows);
        Assert.Equal([@"..\images\scan_card01.jpg", @"..\images\scan_card02.jpg"], ReadColumn(ListingColumns.Images));
    }

    [Fact]
    public void AppendCardRows_WithRecognisedDetails_FillsCardColumns()
    {
        var details = new RecognizedCard("Riftbound", "Flame Chompers", "Origins", "OGN-006", "Common", "English");

        _appender.AppendCardRows(_sheetPath, [Row("a.jpg", details), Row("b.jpg")]);

        Assert.Equal(["Flame Chompers"], ReadColumn(ListingColumns.CardName));
        Assert.Equal(["Riftbound"], ReadColumn(ListingColumns.Game));
        Assert.Equal(["OGN-006"], ReadColumn(ListingColumns.CardNumber));
        Assert.Equal(["Origins"], ReadColumn(ListingColumns.SetName));
        Assert.Equal(["Common"], ReadColumn(ListingColumns.Rarity));
        Assert.Equal(2, ReadColumn(ListingColumns.Images).Count);
    }

    [Fact]
    public void AppendCardRows_AddsAfterExistingRowsAndSkipsImagesAlreadyListed()
    {
        _appender.AppendCardRows(_sheetPath, [Row("a.jpg")]);

        var addedRows = _appender.AppendCardRows(_sheetPath, [Row("a.jpg"), Row("b.jpg")]);

        Assert.Equal(1, addedRows);
        Assert.Equal([@"..\images\a.jpg", @"..\images\b.jpg"], ReadColumn(ListingColumns.Images));
    }

    [Fact]
    public void AppendCardRows_SheetWithoutImagesColumn_Throws()
    {
        var sheetPath = _directory.GetFilePath("other.xlsx");
        using (var workbook = new XLWorkbook())
        {
            workbook.AddWorksheet("Sheet1").Cell(1, 1).Value = "CardName";
            workbook.SaveAs(sheetPath);
        }

        Assert.Throws<InvalidOperationException>(() => _appender.AppendCardRows(sheetPath, [Row("a.jpg")]));
    }

    public void Dispose() => _directory.Dispose();

    private ScannedCardRow Row(string imageName, RecognizedCard? details = null) =>
        new(_directory.GetFilePath($@"images\{imageName}"), details);

    private List<string> ReadColumn(ColumnDefinition column)
    {
        using var workbook = new XLWorkbook(_sheetPath);

        return workbook.Worksheet(ListingWorkbookLayout.CardsSheetName)
            .Column(ListingColumns.IndexOf(column) + 1).CellsUsed().Skip(1)
            .Select(cell => cell.GetString())
            .ToList();
    }
}
