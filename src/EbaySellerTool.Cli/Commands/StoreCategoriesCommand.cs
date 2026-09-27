using System.CommandLine;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Stores;
using EbaySellerTool.Core.Excel;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal sealed class StoreCategoriesCommand(IStoreCategoryCatalog storeCategoryCatalog) : ICliCommand
{
    private const int IndentPerLevel = 2;

    public Command Build()
    {
        var command = new Command(
            "store-categories",
            $"Show your eBay Store categories and the path to put in the {ListingColumns.StoreCategory.Header} column.");
        command.SetAction((_, cancellationToken) => ExecuteAsync(cancellationToken));

        return command;
    }

    private async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await storeCategoryCatalog.RefreshAsync(cancellationToken);
            Render(categories);
            AnsiConsole.MarkupLine("Saved for sheet validation and new templates.");
            return ExitCodes.Success;
        }
        catch (EbayNotSignedInException exception)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
            return ExitCodes.InvalidInput;
        }
        catch (EbayApiException exception)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
            return ExitCodes.ValidationFailed;
        }
    }

    private static void Render(IReadOnlyList<StoreCategory> categories)
    {
        if (categories.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Your eBay Store has no categories.[/]");
            return;
        }

        var table = new Table().AddColumns("Category", $"{ListingColumns.StoreCategory.Header} value", "ID");

        foreach (var category in categories)
        {
            var indent = new string(' ', Math.Max(0, category.Level - 1) * IndentPerLevel);
            table.AddRow(new Text(indent + category.Name), new Text(category.Path), new Text(category.Id));
        }

        AnsiConsole.Write(table);
    }
}
