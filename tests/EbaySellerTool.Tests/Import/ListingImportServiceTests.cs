using ClosedXML.Excel;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Import;
using EbaySellerTool.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace EbaySellerTool.Tests.Import;

public sealed class ListingImportServiceTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services = TestSettings.CreateCoreServices();

    private IListingImportService ImportService => _services.GetRequiredService<IListingImportService>();

    [Fact]
    public void Import_Template_HasNoRowsOrErrors()
    {
        var filePath = _directory.GetFilePath("template.xlsx");
        _services.GetRequiredService<IListingTemplateWriter>().Write(filePath);

        var result = ImportService.Import(filePath);

        Assert.Equal(0, result.TotalRows);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_FilledTemplate_SeparatesValidAndInvalidRows()
    {
        _directory.CreateFile(@"images\blue-eyes.jpg");
        var filePath = _directory.GetFilePath("cards.xlsx");
        _services.GetRequiredService<IListingTemplateWriter>().Write(filePath);

        using (var workbook = new XLWorkbook(filePath))
        {
            var sheet = workbook.Worksheet(ListingWorkbookLayout.CardsSheetName);
            FillRow(sheet, 2, cardName: "Blue-Eyes White Dragon", price: 49.95, image: @"images\blue-eyes.jpg");
            FillRow(sheet, 4, cardName: "Dark Magician", price: 0, image: @"images\missing.jpg");
            workbook.Save();
        }

        var result = ImportService.Import(filePath);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal("Blue-Eyes White Dragon", Assert.Single(result.ValidListings).CardName);
        Assert.All(result.Errors, error => Assert.Equal(4, error.RowNumber));
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Import_SheetWithoutRequiredColumns_ReturnsSheetLevelErrors()
    {
        var filePath = _directory.GetFilePath("wrong.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Sheet1");
            sheet.Cell(1, 1).Value = "Card Name";
            sheet.Cell(2, 1).Value = "Dark Magician";
            workbook.SaveAs(filePath);
        }

        var result = ImportService.Import(filePath);

        Assert.True(result.HasErrors);
        Assert.All(result.Errors, error => Assert.Null(error.RowNumber));
        Assert.DoesNotContain(result.Errors, error => error.Column == ListingColumns.CardName.Header);
    }

    public void Dispose()
    {
        _services.Dispose();
        _directory.Dispose();
    }

    private static void FillRow(IXLWorksheet sheet, int rowNumber, string cardName, double price, string image)
    {
        SetCell(sheet, rowNumber, ListingColumns.Game, "Yu-Gi-Oh! TCG");
        SetCell(sheet, rowNumber, ListingColumns.CardName, cardName);
        SetCell(sheet, rowNumber, ListingColumns.CardNumber, $"LOB-EN00{rowNumber}");
        SetCell(sheet, rowNumber, ListingColumns.CardCondition, "NM");
        SetCell(sheet, rowNumber, ListingColumns.Price, price);
        SetCell(sheet, rowNumber, ListingColumns.Images, image);
    }

    private static void SetCell(IXLWorksheet sheet, int rowNumber, ColumnDefinition column, XLCellValue value) =>
        sheet.Cell(rowNumber, ListingColumns.IndexOf(column) + 1).Value = value;
}
