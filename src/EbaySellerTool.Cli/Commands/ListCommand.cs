using System.CommandLine;
using System.Text.Json;
using EbaySellerTool.Cli.Rendering;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Import;
using EbaySellerTool.Core.Listing;
using EbaySellerTool.Core.Listing.DryRun;
using EbaySellerTool.Core.Reporting;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal sealed class ListCommand(
    IListingImportService importService,
    IDryRunPlanner dryRunPlanner,
    IListingReportWriter reportWriter,
    IOptions<ListingDefaultsOptions> listingDefaults,
    ImportResultRenderer importRenderer,
    ListingRunRenderer runRenderer) : ICliCommand
{
    private const string DryRunFilePrefix = "dryrun";
    private const string ResultsFilePrefix = "results";

    public Command Build()
    {
        var fileArgument = new Argument<FileInfo>("file") { Description = "Excel sheet of cards to list (.xlsx)." };
        var dryRunOption = new Option<bool>("--dry-run") { Description = "Build the eBay requests and save them to a JSON file without calling eBay." };

        var command = new Command("list", "List the cards in an Excel sheet on eBay.") { fileArgument, dryRunOption };
        command.SetAction(parseResult => Execute(parseResult.GetRequiredValue(fileArgument), parseResult.GetValue(dryRunOption)));

        return command;
    }

    private int Execute(FileInfo file, bool isDryRun)
    {
        if (ExcelFiles.FindInputFileProblem(file) is { } problem)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{problem}[/]");
            return ExitCodes.InvalidInput;
        }

        if (!isDryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Live listing needs the eBay API connection, which isn't set up yet.[/] Use [bold]--dry-run[/] to preview the requests.");
            return ExitCodes.NotAvailable;
        }

        var importResult = importService.Import(file.FullName);

        if (importResult.TotalRows == 0)
        {
            importRenderer.Render(importResult);
            return importResult.HasErrors ? ExitCodes.ValidationFailed : ExitCodes.Success;
        }

        WarnAboutMissingSettings();
        return RunDryRun(file, importResult);
    }

    private int RunDryRun(FileInfo file, ListingImportResult importResult)
    {
        var timestamp = DateTime.Now;

        var planPath = ExcelFiles.OutputPathNextTo(file, DryRunFilePrefix, timestamp, ".json");
        var plan = dryRunPlanner.CreatePlan(importResult.ValidListings);
        File.WriteAllText(planPath, JsonSerializer.Serialize(plan, EbayJson.IndentedOptions));

        var result = new ListingRunResult(
        [
            .. ListingOutcomes.ForDryRun(importResult.ValidListings),
            .. ListingOutcomes.ForInvalidRows(importResult.Errors)
        ]);

        var reportPath = ExcelFiles.OutputPathNextTo(file, ResultsFilePrefix, timestamp, ExcelFiles.Extension);
        reportWriter.Write(result, reportPath);

        runRenderer.Render(result);
        AnsiConsole.MarkupLineInterpolated($"eBay requests: [link]{planPath}[/]");
        AnsiConsole.MarkupLineInterpolated($"Results:       [link]{reportPath}[/]");

        return result.HasFailures ? ExitCodes.ValidationFailed : ExitCodes.Success;
    }

    private void WarnAboutMissingSettings()
    {
        var missingSettings = listingDefaults.Value.GetMissingRequiredSettings();

        if (missingSettings.Count > 0)
        {
            AnsiConsole.MarkupLineInterpolated(
                $"[yellow]Not configured yet (required for live listing):[/] {string.Join(", ", missingSettings)}");
        }
    }
}
