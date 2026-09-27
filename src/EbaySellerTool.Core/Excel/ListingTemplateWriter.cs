using ClosedXML.Excel;
using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Ebay.Stores;

namespace EbaySellerTool.Core.Excel;

public sealed class ListingTemplateWriter(IStoreCategoryCache storeCategoryCache) : IListingTemplateWriter
{
    private const int HeaderRowNumber = 1;
    private const int ConditionListColumn = 1;
    private const int StoreCategoryListColumn = 2;
    private const int LastInputRowNumber = 2000;
    private const double MinColumnWidth = 14;
    private const double MaxColumnWidth = 50;

    private static readonly XLColor RequiredHeaderColor = XLColor.FromHtml("#F4B183");
    private static readonly XLColor OptionalHeaderColor = XLColor.FromHtml("#D9E1F2");

    public void Write(string filePath)
    {
        using var workbook = new XLWorkbook();

        var cardsSheet = AddCardsSheet(workbook);
        AddInstructionsSheet(workbook);
        var listsSheet = workbook.AddWorksheet(ListingWorkbookLayout.ListsSheetName);
        listsSheet.Hide();

        // Warning (not Stop) so short codes like "NM" can still be typed in.
        AddDropdown(cardsSheet, ListingColumns.CardCondition, listsSheet, ConditionListColumn, CardConditions.AllDisplayNames, XLErrorStyle.Warning);

        var storeCategoryPaths = storeCategoryCache.Load()?.Select(category => category.Path).ToList() ?? [];
        AddDropdown(cardsSheet, ListingColumns.StoreCategory, listsSheet, StoreCategoryListColumn, storeCategoryPaths, XLErrorStyle.Stop);

        workbook.SaveAs(filePath);
    }

    private static IXLWorksheet AddCardsSheet(XLWorkbook workbook)
    {
        var sheet = workbook.AddWorksheet(ListingWorkbookLayout.CardsSheetName);

        for (var index = 0; index < ListingColumns.All.Count; index++)
        {
            var column = ListingColumns.All[index];
            var cell = sheet.Cell(HeaderRowNumber, index + 1);

            cell.Value = column.Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = column.IsRequired ? RequiredHeaderColor : OptionalHeaderColor;
            cell.CreateComment().AddText(column.Description);
            sheet.Column(index + 1).Width = Math.Clamp(column.Example.Length + 2, MinColumnWidth, MaxColumnWidth);
        }

        FormatInputColumn(sheet, ListingColumns.Price, "0.00");
        FormatInputColumn(sheet, ListingColumns.Quantity, "0");
        FormatInputColumn(sheet, ListingColumns.CategoryId, "@");
        sheet.SheetView.FreezeRows(HeaderRowNumber);

        return sheet;
    }

    private static void AddInstructionsSheet(XLWorkbook workbook)
    {
        var sheet = workbook.AddWorksheet(ListingWorkbookLayout.InstructionsSheetName);
        sheet.Cell(1, 1).InsertTable(ListingColumns.All.Select(column => new
        {
            Column = column.Header,
            Required = column.IsRequired ? "Yes" : "No",
            column.Description,
            column.Example
        }));

        var notesRow = ListingColumns.All.Count + 3;
        sheet.Cell(notesRow, 1).Value = "Extra item specifics";
        sheet.Cell(notesRow, 1).Style.Font.Bold = true;
        sheet.Cell(notesRow, 3).Value =
            $"Add a column named '{ListingColumns.AspectPrefix}<Name>' (for example '{ListingColumns.AspectPrefix}Edition') to send any other eBay item specific.";

        sheet.Columns().AdjustToContents(1, notesRow - 1, MinColumnWidth, MaxColumnWidth * 2);
    }

    private static void AddDropdown(
        IXLWorksheet cardsSheet,
        ColumnDefinition column,
        IXLWorksheet listsSheet,
        int listColumn,
        IReadOnlyList<string> options,
        XLErrorStyle errorStyle)
    {
        if (options.Count == 0)
        {
            return;
        }

        for (var index = 0; index < options.Count; index++)
        {
            listsSheet.Cell(index + 1, listColumn).Value = options[index];
        }

        var validation = InputRange(cardsSheet, column).CreateDataValidation();
        validation.List(listsSheet.Range(1, listColumn, options.Count, listColumn), true);
        validation.ErrorStyle = errorStyle;
    }

    private static void FormatInputColumn(IXLWorksheet sheet, ColumnDefinition column, string numberFormat) =>
        InputRange(sheet, column).Style.NumberFormat.Format = numberFormat;

    private static IXLRange InputRange(IXLWorksheet sheet, ColumnDefinition column)
    {
        var columnNumber = ListingColumns.IndexOf(column) + 1;
        return sheet.Range(HeaderRowNumber + 1, columnNumber, LastInputRowNumber, columnNumber);
    }
}
