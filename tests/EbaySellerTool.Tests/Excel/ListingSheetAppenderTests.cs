using ClosedXML.Excel;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Excel;

public sealed class ListingSheetAppenderTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ListingSheetAppender _appender = new();
    private readonly string _sheetPath;

    public ListingSheetAppenderTests()
    {
        _sheetPath = _directory.GetFilePath("cards.xlsx");
        new ListingTemplateWriter().Write(_sheetPath);
    }

    [Fact]
    public void AppendImageRows_AddsRowPerImageWithPathRelativeToSheet()
    {
        string[] images = [_directory.GetFilePath(@"images\scan_card01.jpg"), _directory.GetFilePath(@"images\scan_card02.jpg")];

        var addedRows = _appender.AppendImageRows(_sheetPath, images);

        Assert.Equal(2, addedRows);
        Assert.Equal([@"images\scan_card01.jpg", @"images\scan_card02.jpg"], ReadImagesColumn());
    }

    [Fact]
    public void AppendImageRows_AddsAfterExistingRowsAndSkipsImagesAlreadyListed()
    {
        _appender.AppendImageRows(_sheetPath, [_directory.GetFilePath(@"images\a.jpg")]);

        var addedRows = _appender.AppendImageRows(_sheetPath, [_directory.GetFilePath(@"images\a.jpg"), _directory.GetFilePath(@"images\b.jpg")]);

        Assert.Equal(1, addedRows);
        Assert.Equal([@"images\a.jpg", @"images\b.jpg"], ReadImagesColumn());
    }

    [Fact]
    public void AppendImageRows_SheetWithoutImagesColumn_Throws()
    {
        var sheetPath = _directory.GetFilePath("other.xlsx");
        using (var workbook = new XLWorkbook())
        {
            workbook.AddWorksheet("Sheet1").Cell(1, 1).Value = "CardName";
            workbook.SaveAs(sheetPath);
        }

        Assert.Throws<InvalidOperationException>(() => _appender.AppendImageRows(sheetPath, [_directory.GetFilePath("a.jpg")]));
    }

    public void Dispose() => _directory.Dispose();

    private List<string> ReadImagesColumn()
    {
        using var workbook = new XLWorkbook(_sheetPath);
        var column = ListingColumns.IndexOf(ListingColumns.Images) + 1;

        return workbook.Worksheet(ListingWorkbookLayout.CardsSheetName)
            .Column(column).CellsUsed().Skip(1)
            .Select(cell => cell.GetString())
            .ToList();
    }
}
