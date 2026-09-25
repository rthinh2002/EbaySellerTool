namespace EbaySellerTool.Core.Import;

public interface IListingImportService
{
    ListingImportResult Import(string filePath);
}
