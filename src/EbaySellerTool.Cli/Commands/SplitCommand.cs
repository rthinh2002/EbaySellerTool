using System.Collections.Concurrent;
using System.CommandLine;
using EbaySellerTool.Cli.Rendering;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Recognition;
using EbaySellerTool.Core.Scanning;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal sealed class SplitCommand(
    IScanSplitter scanSplitter,
    ICardRecognizer cardRecognizer,
    IListingSheetAppender sheetAppender,
    IListingTemplateWriter templateWriter,
    IOptions<CardRecognitionOptions> recognitionOptions,
    ScanSplitRenderer renderer) : ICliCommand
{
    // Enough to keep the scan moving without tripping API rate limits.
    private const int MaxConcurrentRecognitions = 4;

    public Command Build()
    {
        var inputArgument = new Argument<FileSystemInfo>("input") { Description = "A scanned image, or a folder of scans." };
        var outputOption = new Option<DirectoryInfo>("--output", "-o")
        {
            Description = "Folder to save the card images in.",
            DefaultValueFactory = _ => new DirectoryInfo(WorkspaceFolders.Images)
        };
        var sheetOption = new Option<FileInfo?>("--sheet", "-s")
        {
            Description = "Excel sheet to add a row per card to, with the image and card details filled in. Created if it doesn't exist."
        };
        var noRecognizeOption = new Option<bool>("--no-recognize")
        {
            Description = "Only fill in the Images column; don't read card details with Claude."
        };

        var command = new Command("split", "Cut a flatbed scan of several cards into one image per card.")
        {
            inputArgument, outputOption, sheetOption, noRecognizeOption
        };
        command.SetAction((parseResult, cancellationToken) => ExecuteAsync(
            parseResult.GetRequiredValue(inputArgument),
            parseResult.GetRequiredValue(outputOption),
            parseResult.GetValue(sheetOption),
            !parseResult.GetValue(noRecognizeOption),
            cancellationToken));

        return command;
    }

    private async Task<int> ExecuteAsync(
        FileSystemInfo input, DirectoryInfo outputDirectory, FileInfo? sheet, bool shouldRecognize, CancellationToken cancellationToken)
    {
        if (sheet is not null && !ExcelFiles.HasExcelExtension(sheet))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]The sheet must be an {ExcelFiles.Extension} file:[/] {sheet.FullName}");
            return ExitCodes.InvalidInput;
        }

        var scanPaths = FindScans(input);

        if (scanPaths.Count == 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]No scans found at[/] {input.FullName}");
            return ExitCodes.InvalidInput;
        }

        var results = scanPaths.Select(scanPath => scanSplitter.Split(scanPath, outputDirectory.FullName)).ToList();
        renderer.Render(results, outputDirectory.FullName);

        if (sheet is not null)
        {
            var imagePaths = results.SelectMany(result => result.Cards).Select(card => card.FilePath).ToList();
            var rows = shouldRecognize ? await RecognizeCardsAsync(imagePaths, cancellationToken) : imagePaths.Select(path => new ScannedCardRow(path)).ToList();
            AddRowsToSheet(sheet, rows);
        }

        return results.All(result => result.IsSuccess) ? ExitCodes.Success : ExitCodes.ValidationFailed;
    }

    private async Task<IReadOnlyList<ScannedCardRow>> RecognizeCardsAsync(IReadOnlyList<string> imagePaths, CancellationToken cancellationToken)
    {
        if (!recognitionOptions.Value.HasApiKey)
        {
            AnsiConsole.MarkupLine("[yellow]No Anthropic API key set, so card details weren't read.[/] See the README to add one, or use --no-recognize.");
            return [.. imagePaths.Select(path => new ScannedCardRow(path))];
        }

        var recognitions = new ConcurrentDictionary<string, CardRecognitionResult>();

        await AnsiConsole.Status().StartAsync($"Reading {imagePaths.Count} card(s) with Claude...", async _ =>
            await Parallel.ForEachAsync(
                imagePaths,
                new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentRecognitions, CancellationToken = cancellationToken },
                async (imagePath, token) => recognitions[imagePath] = await cardRecognizer.RecognizeAsync(imagePath, token)));

        var ordered = imagePaths.Select(path => (ImagePath: path, Result: recognitions[path])).ToList();
        renderer.RenderRecognitions(ordered);

        return [.. ordered.Select(entry => new ScannedCardRow(entry.ImagePath, entry.Result.Card))];
    }

    private static List<string> FindScans(FileSystemInfo input) => input switch
    {
        FileInfo { Exists: true } file => [file.FullName],
        DirectoryInfo { Exists: true } directory => directory.EnumerateFiles()
            .Where(file => ScanSplitter.SupportedScanExtensions.Contains(file.Extension))
            .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .Select(file => file.FullName)
            .ToList(),
        _ => []
    };

    private void AddRowsToSheet(FileInfo sheet, IReadOnlyList<ScannedCardRow> rows)
    {
        try
        {
            if (!sheet.Exists)
            {
                sheet.Directory?.Create();
                templateWriter.Write(sheet.FullName);
            }

            var addedRows = sheetAppender.AppendCardRows(sheet.FullName, rows);
            var skippedRows = rows.Count - addedRows;
            AnsiConsole.MarkupLineInterpolated($"[green]Added {addedRows} row(s) to[/] {sheet.FullName}");

            if (skippedRows > 0)
            {
                AnsiConsole.MarkupLineInterpolated($"Skipped {skippedRows} image(s) the sheet already lists.");
            }
        }
        catch (IOException)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Couldn't update {sheet.FullName}.[/] Close it in Excel and run the command again.");
        }
    }
}
