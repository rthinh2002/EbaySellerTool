using ClosedXML.Excel;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Tests.TestSupport;

namespace EbaySellerTool.Tests.Excel;

public sealed class ListingTemplateWriterTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    [Fact]
    public void Write_WithSavedStoreCategories_AddsStoreCategoryDropdown()
    {
        var filePath = _directory.GetFilePath("template.xlsx");

        new ListingTemplateWriter(InMemoryStoreCategoryCache.WithPaths("/YU-GI-OH! Singles", "/Riftbound")).Write(filePath);

        using var workbook = new XLWorkbook(filePath);
        var validation = StoreCategoryCell(workbook).GetDataValidation();
        Assert.Equal(XLAllowedValues.List, validation.AllowedValues);

        var lists = workbook.Worksheet(ListingWorkbookLayout.ListsSheetName);
        Assert.Equal(["/YU-GI-OH! Singles", "/Riftbound"], [lists.Cell(1, 2).GetString(), lists.Cell(2, 2).GetString()]);
    }

    [Fact]
    public void Write_WithoutSavedStoreCategories_LeavesStoreCategoryFreeText()
    {
        var filePath = _directory.GetFilePath("template.xlsx");

        new ListingTemplateWriter(new InMemoryStoreCategoryCache()).Write(filePath);

        using var workbook = new XLWorkbook(filePath);
        Assert.False(StoreCategoryCell(workbook).HasDataValidation);
    }

    public void Dispose() => _directory.Dispose();

    private static IXLCell StoreCategoryCell(XLWorkbook workbook) =>
        workbook.Worksheet(ListingWorkbookLayout.CardsSheetName).Cell(2, ListingColumns.IndexOf(ListingColumns.StoreCategory) + 1);
}
