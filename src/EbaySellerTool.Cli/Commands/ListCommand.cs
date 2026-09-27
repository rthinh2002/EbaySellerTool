using System.CommandLine;
using System.Text.Json;
using EbaySellerTool.Cli.Rendering;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Stores;
using EbaySellerTool.Core.Import;
using EbaySellerTool.Core.Listing;
using EbaySellerTool.Core.Listing.DryRun;
using EbaySellerTool.Core.Reporting;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal sealed class ListCommand(
    IListingImportService importService,
    IListingService listingService,
    IStoreCategoryCatalog storeCategoryCatalog,
    IDryRunPlanner dryRunPlanner,
    IListingReportWriter reportWriter,
    IOptions<EbayOptions> ebayOptions,
    IOptions<ListingDefaultsOptions> listingDefaults,
    ImportResultRenderer importRenderer,
    ListingRunRenderer runRenderer) : ICliCommand
{
    private const string DryRunFilePrefix = "dryrun";
    private const string ResultsFilePrefix = "results";

    private readonly EbayOptions _ebay = ebayOptions.Value;

    public Command Build()
    {
        var fileArgument = new Argument<FileInfo>("file") { Description = "Excel sheet of cards to list (.xlsx)." };
        var dryRunOption = new Option<bool>("--dry-run") { Description = "Build the eBay requests and save them to a JSON file without calling eBay." };

        var command = new Command("list", "List the cards in an Excel sheet on eBay.") { fileArgument, dryRunOption };
        command.SetAction((parseResult, cancellationToken) =>
            ExecuteAsync(parseResult.GetRequiredValue(fileArgument), parseResult.GetValue(dryRunOption), cancellationToken));

        return command;
    }

    private async Task<int> ExecuteAsync(FileInfo file, bool isDryRun, CancellationToken cancellationToken)
    {
        if (ExcelFiles.FindInputFileProblem(file) is { } problem)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{problem}[/]");
            return ExitCodes.InvalidInput;
        }

        var missingSettings = listingDefaults.Value.GetMissingRequiredSettings();

        if (!isDryRun && missingSettings.Count > 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Not configured yet:[/] {string.Join(", ", missingSettings)}");
            AnsiConsole.MarkupLine("Run [bold]ebaytool setup[/] first.");
            return ExitCodes.InvalidInput;
        }

        if (!isDryRun)
        {
            await StoreCategoryRefresh.TryRefreshAsync(storeCategoryCatalog, cancellationToken);
        }

        var importResult = importService.Import(file.FullName);

        if (importResult.TotalRows == 0)
        {
            importRenderer.Render(importResult);
            return importResult.HasErrors ? ExitCodes.ValidationFailed : ExitCodes.Success;
        }

        return isDryRun
            ? RunDryRun(file, importResult, missingSettings)
            : await RunLiveAsync(file, importResult, cancellationToken);
    }

    private int RunDryRun(FileInfo file, ListingImportResult importResult, IReadOnlyList<string> missingSettings)
    {
        if (missingSettings.Count > 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[yellow]Not configured yet (required for live listing):[/] {string.Join(", ", missingSettings)}");
        }

        var timestamp = DateTime.Now;
        var planPath = WorkspaceFolders.OutputFilePath(WorkspaceFolders.DryRuns, file, DryRunFilePrefix, timestamp, ".json");
        var plan = dryRunPlanner.CreatePlan(importResult.ValidListings);
        File.WriteAllText(planPath, JsonSerializer.Serialize(plan, EbayJson.IndentedOptions));

        var result = CombineWithInvalidRows(ListingOutcomes.ForDryRun(importResult.ValidListings), importResult);
        var exitCode = Report(file, result, timestamp);
        AnsiConsole.MarkupLineInterpolated($"eBay requests: [link]{planPath}[/]");

        return exitCode;
    }

    private async Task<int> RunLiveAsync(FileInfo file, ListingImportResult importResult, CancellationToken cancellationToken)
    {
        var timestamp = DateTime.Now;
        IEnumerable<ListingOutcome> outcomes = [];

        try
        {
            if (importResult.ValidListings.Count > 0)
            {
                var runResult = await AnsiConsole.Status().StartAsync(
                    $"Listing {importResult.ValidListings.Count} card(s) on eBay {_ebay.Environment}...",
                    _ => listingService.ListAsync(importResult.ValidListings, cancellationToken));
                outcomes = runResult.Outcomes;
            }
        }
        catch (EbayNotSignedInException exception)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
            return ExitCodes.InvalidInput;
        }

        return Report(file, CombineWithInvalidRows(outcomes, importResult), timestamp);
    }

    private static ListingRunResult CombineWithInvalidRows(IEnumerable<ListingOutcome> outcomes, ListingImportResult importResult) =>
        new([.. outcomes, .. ListingOutcomes.ForInvalidRows(importResult.Errors)]);

    private int Report(FileInfo file, ListingRunResult result, DateTime timestamp)
    {
        var reportPath = WorkspaceFolders.OutputFilePath(WorkspaceFolders.Results, file, ResultsFilePrefix, timestamp, ExcelFiles.Extension);
        reportWriter.Write(result, reportPath);

        runRenderer.Render(result);
        AnsiConsole.MarkupLineInterpolated($"Results: [link]{reportPath}[/]");

        return result.HasFailures ? ExitCodes.ValidationFailed : ExitCodes.Success;
    }
}
