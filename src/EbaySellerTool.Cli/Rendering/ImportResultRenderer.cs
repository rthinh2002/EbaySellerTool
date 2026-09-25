using System.Globalization;
using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Import;
using EbaySellerTool.Core.Validation;
using Spectre.Console;

namespace EbaySellerTool.Cli.Rendering;

internal sealed class ImportResultRenderer
{
    private const string SheetLevelRowLabel = "-";
    private static readonly CultureInfo AustralianCulture = CultureInfo.GetCultureInfo("en-AU");

    public void Render(ListingImportResult result)
    {
        if (result.ValidListings.Count > 0)
        {
            AnsiConsole.Write(BuildValidListingsTable(result.ValidListings));
        }

        if (result.HasErrors)
        {
            AnsiConsole.Write(BuildErrorsTable(result.Errors));
        }

        RenderSummary(result);
    }

    private static Table BuildValidListingsTable(IReadOnlyList<CardListing> listings)
    {
        var table = new Table()
            .Title("[green]Ready to list[/]")
            .AddColumns("Row", "SKU", "Title", "Condition", "Qty", "Price", "Images");

        foreach (var listing in listings)
        {
            table.AddRow(
                Cell(listing.RowNumber.ToString(CultureInfo.InvariantCulture)),
                Cell(listing.Sku),
                Cell(listing.Title),
                Cell(listing.Condition.ToCode()),
                Cell(listing.Quantity.ToString(CultureInfo.InvariantCulture)),
                Cell(listing.Price.ToString("C", AustralianCulture)),
                Cell(listing.ImagePaths.Count.ToString(CultureInfo.InvariantCulture)));
        }

        return table;
    }

    private static Table BuildErrorsTable(IReadOnlyList<ValidationError> errors)
    {
        var table = new Table()
            .Title("[red]Errors[/]")
            .AddColumns("Row", "Column", "Problem");

        foreach (var error in errors)
        {
            table.AddRow(
                Cell(error.RowNumber?.ToString(CultureInfo.InvariantCulture) ?? SheetLevelRowLabel),
                Cell(error.Column ?? string.Empty),
                Cell(error.Message));
        }

        return table;
    }

    private static void RenderSummary(ListingImportResult result)
    {
        AnsiConsole.MarkupLineInterpolated(
            $"Rows: [bold]{result.TotalRows}[/]  Valid: [green]{result.ValidListings.Count}[/]  Invalid: [red]{result.InvalidRowCount}[/]");

        if (result.TotalRows == 0 && !result.HasErrors)
        {
            AnsiConsole.MarkupLine("[yellow]The sheet has no card rows.[/]");
        }
    }

    private static Text Cell(string value) => new(value);
}
