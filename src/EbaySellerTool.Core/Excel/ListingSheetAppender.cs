using ClosedXML.Excel;

namespace EbaySellerTool.Core.Excel;

public sealed class ListingSheetAppender : IListingSheetAppender
{
    public int AppendImageRows(string sheetPath, IReadOnlyList<string> imagePaths)
    {
        using var workbook = new XLWorkbook(sheetPath);
        var sheet = workbook.TryGetWorksheet(ListingWorkbookLayout.CardsSheetName, out var cardsSheet) ? cardsSheet : workbook.Worksheet(1);

        var imagesColumn = FindColumnNumber(sheet, ListingColumns.Images)
            ?? throw new InvalidOperationException($"The sheet has no '{ListingColumns.Images.Header}' column.");

        var sheetDirectory = Path.GetDirectoryName(Path.GetFullPath(sheetPath))!;
        var existingImages = ReadColumnValues(sheet, imagesColumn);
        var newImages = imagePaths
            .Select(imagePath => Path.GetRelativePath(sheetDirectory, imagePath))
            .Where(relativePath => !existingImages.Contains(relativePath))
            .ToList();

        var nextRowNumber = (sheet.LastRowUsed()?.RowNumber() ?? 1) + 1;

        foreach (var relativePath in newImages)
        {
            sheet.Cell(nextRowNumber++, imagesColumn).Value = relativePath;
        }

        workbook.Save();
        return newImages.Count;
    }

    private static int? FindColumnNumber(IXLWorksheet sheet, ColumnDefinition column) =>
        sheet.FirstRowUsed()?.CellsUsed()
            .FirstOrDefault(cell => ListingColumns.NormalizeHeader(cell.GetString()) == column.Key)?
            .Address.ColumnNumber;

    private static HashSet<string> ReadColumnValues(IXLWorksheet sheet, int columnNumber) =>
        sheet.Column(columnNumber).CellsUsed()
            .Skip(1)
            .Select(cell => cell.GetString().Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
