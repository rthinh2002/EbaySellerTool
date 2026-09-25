using System.Globalization;
using ClosedXML.Excel;

namespace EbaySellerTool.Core.Excel;

public sealed class ListingSheetReader : IListingSheetReader
{
    public ListingSheet Read(string filePath)
    {
        // FileShare.ReadWrite lets the sheet be read while it is still open in Excel.
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var workbook = new XLWorkbook(stream);

        var worksheet = FindCardsWorksheet(workbook);
        var headerRow = worksheet.FirstRowUsed();

        if (headerRow is null)
        {
            return ListingSheet.Empty;
        }

        var headers = ReadHeaders(headerRow);
        var rows = worksheet.RowsUsed()
            .Where(row => row.RowNumber() > headerRow.RowNumber())
            .Select(row => ReadRow(row, headers))
            .OfType<ListingRow>()
            .ToList();

        return new ListingSheet([.. headers.Values], rows);
    }

    private static IXLWorksheet FindCardsWorksheet(XLWorkbook workbook) =>
        workbook.TryGetWorksheet(ListingWorkbookLayout.CardsSheetName, out var worksheet)
            ? worksheet
            : workbook.Worksheet(1);

    private static Dictionary<int, string> ReadHeaders(IXLRow headerRow) =>
        headerRow.CellsUsed()
            .Select(cell => (Column: cell.Address.ColumnNumber, Header: cell.GetString().Trim()))
            .Where(header => header.Header.Length > 0)
            .ToDictionary(header => header.Column, header => header.Header);

    private static ListingRow? ReadRow(IXLRow row, IReadOnlyDictionary<int, string> headers)
    {
        var values = new Dictionary<string, string>();
        var aspects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (columnNumber, header) in headers)
        {
            var text = ReadCellText(row.Cell(columnNumber));

            if (ListingColumns.IsAspectHeader(header))
            {
                if (text.Length > 0)
                {
                    aspects[ListingColumns.GetAspectName(header)] = text;
                }
            }
            else
            {
                values[ListingColumns.NormalizeHeader(header)] = text;
            }
        }

        var isBlank = aspects.Count == 0 && values.Values.All(value => value.Length == 0);
        return isBlank ? null : new ListingRow(row.RowNumber(), values, aspects);
    }

    private static string ReadCellText(IXLCell cell) =>
        cell.Value.IsNumber
            ? cell.Value.GetNumber().ToString(CultureInfo.InvariantCulture)
            : cell.GetString().Trim();
}
