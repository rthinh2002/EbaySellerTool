using ClosedXML.Excel;
using EbaySellerTool.Core.Listing;
using EbaySellerTool.Core.Reporting;
using EbaySellerTool.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Tests.Reporting;

public class ListingReportWriterTests
{
    [Fact]
    public void Write_CreatesRowPerOutcomeWithListingLinkAndErrors()
    {
        using var directory = new TemporaryDirectory();
        var filePath = directory.GetFilePath("results.xlsx");
        var result = new ListingRunResult(
        [
            new ListingOutcome { RowNumber = 2, Sku = "A", Title = "Card A", Status = ListingStatus.Listed, ListingId = "12345" },
            new ListingOutcome { RowNumber = 3, Status = ListingStatus.Invalid, Errors = ["Price: bad", "Images: missing"] }
        ]);

        new ListingReportWriter(Options.Create(TestSettings.Ebay())).Write(result, filePath);

        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(ListingReportWriter.SheetName);
        Assert.Equal("Listed", sheet.Cell(2, 4).GetString());
        Assert.Equal("https://www.ebay.com.au/itm/12345", sheet.Cell(2, 6).GetString());
        Assert.True(sheet.Cell(2, 6).HasHyperlink);
        Assert.Equal("Invalid", sheet.Cell(3, 4).GetString());
        Assert.Equal("Price: bad\nImages: missing", sheet.Cell(3, 8).GetString());
    }
}
