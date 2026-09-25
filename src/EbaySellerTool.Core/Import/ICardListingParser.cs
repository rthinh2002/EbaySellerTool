using EbaySellerTool.Core.Excel;

namespace EbaySellerTool.Core.Import;

public interface ICardListingParser
{
    /// <param name="imageBaseDirectory">Folder that relative image paths are resolved from.</param>
    RowParseResult Parse(ListingRow row, string imageBaseDirectory);
}
