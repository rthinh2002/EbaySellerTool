using ClosedXML.Excel;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Listing;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Core.Reporting;

public sealed class ListingReportWriter(IOptions<EbayOptions> ebayOptions) : IListingReportWriter
{
    public const string SheetName = "Results";

    private const double MaxColumnWidth = 80;
    private const char CellLineBreak = '\n';
    private static readonly string[] Headers = ["Row", "SKU", "Title", "Status", "Listing ID", "Listing URL", "Offer ID", "Errors", "Warnings"];

    private static readonly IReadOnlyDictionary<ListingStatus, XLColor> StatusColors = new Dictionary<ListingStatus, XLColor>
    {
        [ListingStatus.Listed] = XLColor.FromHtml("#C6EFCE"),
        [ListingStatus.Revised] = XLColor.FromHtml("#C6EFCE"),
        [ListingStatus.DryRun] = XLColor.FromHtml("#DDEBF7"),
        [ListingStatus.Failed] = XLColor.FromHtml("#FFC7CE"),
        [ListingStatus.Invalid] = XLColor.FromHtml("#FFC7CE")
    };

    private readonly EbayOptions _ebay = ebayOptions.Value;

    public void Write(ListingRunResult result, string filePath)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(SheetName);

        WriteHeader(sheet);

        for (var index = 0; index < result.Outcomes.Count; index++)
        {
            WriteOutcome(sheet.Row(index + 2), result.Outcomes[index]);
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(minWidth: 8, maxWidth: MaxColumnWidth);
        workbook.SaveAs(filePath);
    }

    private static void WriteHeader(IXLWorksheet sheet)
    {
        for (var index = 0; index < Headers.Length; index++)
        {
            var cell = sheet.Cell(1, index + 1);
            cell.Value = Headers[index];
            cell.Style.Font.Bold = true;
        }
    }

    private void WriteOutcome(IXLRow row, ListingOutcome outcome)
    {
        row.Cell(1).Value = outcome.RowNumber;
        row.Cell(2).Value = outcome.Sku;
        row.Cell(3).Value = outcome.Title;
        row.Cell(4).Value = outcome.Status.ToString();
        row.Cell(4).Style.Fill.BackgroundColor = StatusColors[outcome.Status];
        row.Cell(5).Value = outcome.ListingId;
        WriteListingUrl(row.Cell(6), outcome.ListingId);
        row.Cell(7).Value = outcome.OfferId;
        WriteLines(row.Cell(8), outcome.Errors);
        WriteLines(row.Cell(9), outcome.Warnings);
    }

    private static void WriteLines(IXLCell cell, IReadOnlyList<string> lines)
    {
        cell.Value = string.Join(CellLineBreak, lines);
        cell.Style.Alignment.WrapText = lines.Count > 1;
    }

    private void WriteListingUrl(IXLCell cell, string? listingId)
    {
        var url = listingId is null ? null : EbayListingUrls.ForListing(listingId, _ebay);

        if (url is null)
        {
            return;
        }

        cell.Value = url;
        cell.SetHyperlink(new XLHyperlink(url));
    }
}
