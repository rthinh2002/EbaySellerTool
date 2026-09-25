namespace EbaySellerTool.Core.Excel;

public interface IListingSheetReader
{
    ListingSheet Read(string filePath);
}
