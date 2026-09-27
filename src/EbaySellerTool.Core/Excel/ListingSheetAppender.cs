using ClosedXML.Excel;
using EbaySellerTool.Core.Recognition;

namespace EbaySellerTool.Core.Excel;

public sealed class ListingSheetAppender : IListingSheetAppender
{
    public int AppendCardRows(string sheetPath, IReadOnlyList<ScannedCardRow> cards)
    {
        using var workbook = new XLWorkbook(sheetPath);
        var sheet = workbook.TryGetWorksheet(ListingWorkbookLayout.CardsSheetName, out var cardsSheet) ? cardsSheet : workbook.Worksheet(1);
        var columnNumbers = ReadColumnNumbers(sheet);

        if (!columnNumbers.TryGetValue(ListingColumns.Images.Key, out var imagesColumn))
        {
            throw new InvalidOperationException($"The sheet has no '{ListingColumns.Images.Header}' column.");
        }

        var sheetDirectory = Path.GetDirectoryName(Path.GetFullPath(sheetPath))!;
        var existingImages = ReadColumnValues(sheet, imagesColumn);
        var newCards = cards
            .Select(card => (Card: card, RelativeImagePath: Path.GetRelativePath(sheetDirectory, card.ImagePath)))
            .Where(entry => !existingImages.Contains(entry.RelativeImagePath))
            .ToList();

        var nextRowNumber = (sheet.LastRowUsed()?.RowNumber() ?? 1) + 1;

        foreach (var (card, relativeImagePath) in newCards)
        {
            var row = sheet.Row(nextRowNumber++);
            row.Cell(imagesColumn).Value = relativeImagePath;
            WriteDetails(row, columnNumbers, card.Details);
        }

        workbook.Save();
        return newCards.Count;
    }

    private static void WriteDetails(IXLRow row, IReadOnlyDictionary<string, int> columnNumbers, RecognizedCard? details)
    {
        if (details is null)
        {
            return;
        }

        (ColumnDefinition Column, string? Value)[] values =
        [
            (ListingColumns.Game, details.Game),
            (ListingColumns.CardName, details.CardName),
            (ListingColumns.SetName, details.SetName),
            (ListingColumns.CardNumber, details.CardNumber),
            (ListingColumns.Rarity, details.Rarity),
            (ListingColumns.Language, details.Language)
        ];

        foreach (var (column, value) in values)
        {
            if (value is not null && columnNumbers.TryGetValue(column.Key, out var columnNumber))
            {
                row.Cell(columnNumber).Value = value;
            }
        }
    }

    private static Dictionary<string, int> ReadColumnNumbers(IXLWorksheet sheet) =>
        (sheet.FirstRowUsed()?.CellsUsed() ?? Enumerable.Empty<IXLCell>())
            .GroupBy(cell => ListingColumns.NormalizeHeader(cell.GetString()))
            .ToDictionary(group => group.Key, group => group.First().Address.ColumnNumber);

    private static HashSet<string> ReadColumnValues(IXLWorksheet sheet, int columnNumber) =>
        sheet.Column(columnNumber).CellsUsed()
            .Skip(1)
            .Select(cell => cell.GetString().Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
