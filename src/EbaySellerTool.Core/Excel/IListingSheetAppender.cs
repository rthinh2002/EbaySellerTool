namespace EbaySellerTool.Core.Excel;

public interface IListingSheetAppender
{
    /// <summary>
    /// Adds a row per image with only the Images column filled in, skipping images the sheet already lists.
    /// </summary>
    /// <returns>The number of rows added.</returns>
    int AppendImageRows(string sheetPath, IReadOnlyList<string> imagePaths);
}
