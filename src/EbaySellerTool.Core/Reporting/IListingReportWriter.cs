using EbaySellerTool.Core.Listing;

namespace EbaySellerTool.Core.Reporting;

public interface IListingReportWriter
{
    void Write(ListingRunResult result, string filePath);
}
