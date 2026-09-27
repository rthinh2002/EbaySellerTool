namespace EbaySellerTool.Core.Excel;

public interface IListingSheetAppender
{
    /// <summary>
    /// Adds a row per scanned card with its image and any recognised details, skipping images the sheet already lists.
    /// </summary>
    /// <returns>The number of rows added.</returns>
    int AppendCardRows(string sheetPath, IReadOnlyList<ScannedCardRow> cards);
}
