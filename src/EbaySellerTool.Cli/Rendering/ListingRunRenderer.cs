using System.Globalization;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Listing;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EbaySellerTool.Cli.Rendering;

internal sealed class ListingRunRenderer(IOptions<EbayOptions> ebayOptions)
{
    private static readonly IReadOnlyDictionary<ListingStatus, string> StatusColors = new Dictionary<ListingStatus, string>
    {
        [ListingStatus.Listed] = "green",
        [ListingStatus.Revised] = "green",
        [ListingStatus.DryRun] = "blue",
        [ListingStatus.Failed] = "red",
        [ListingStatus.Invalid] = "red"
    };

    private readonly EbayOptions _ebay = ebayOptions.Value;

    public void Render(ListingRunResult result)
    {
        var table = new Table().AddColumns("Row", "SKU", "Status", "Details");

        foreach (var outcome in result.Outcomes)
        {
            table.AddRow(
                new Text(outcome.RowNumber.ToString(CultureInfo.InvariantCulture)),
                new Text(outcome.Sku ?? string.Empty),
                StatusMarkup(outcome.Status),
                new Text(DescribeDetails(outcome)));
        }

        AnsiConsole.Write(table);
        RenderSummary(result);
    }

    private string DescribeDetails(ListingOutcome outcome)
    {
        if (outcome.Errors.Count > 0)
        {
            return string.Join(Environment.NewLine, outcome.Errors);
        }

        if (outcome.ListingId is null)
        {
            return outcome.Title ?? string.Empty;
        }

        return EbayListingUrls.ForListing(outcome.ListingId, _ebay) ?? outcome.ListingId;
    }

    private static void RenderSummary(ListingRunResult result)
    {
        var parts = Enum.GetValues<ListingStatus>()
            .Select(status => (Status: status, Count: result.Count(status)))
            .Where(entry => entry.Count > 0)
            .Select(entry => $"[{StatusColors[entry.Status]}]{entry.Status}: {entry.Count}[/]");

        AnsiConsole.MarkupLine(string.Join("  ", parts));
    }

    private static IRenderable StatusMarkup(ListingStatus status) =>
        new Markup($"[{StatusColors[status]}]{status}[/]");
}
