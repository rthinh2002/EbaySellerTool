using EbaySellerTool.Core.Recognition;

namespace EbaySellerTool.Core.Excel;

/// <param name="Details">Details read from the image, or null when recognition was skipped or failed.</param>
public sealed record ScannedCardRow(string ImagePath, RecognizedCard? Details = null);
