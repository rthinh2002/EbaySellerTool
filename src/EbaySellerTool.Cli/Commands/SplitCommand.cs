using System.CommandLine;
using EbaySellerTool.Cli.Rendering;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Scanning;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal sealed class SplitCommand(
    IScanSplitter scanSplitter,
    IListingSheetAppender sheetAppender,
    IListingTemplateWriter templateWriter,
    ScanSplitRenderer renderer) : ICliCommand
{
    private const string DefaultOutputDirectory = "images";

    public Command Build()
    {
        var inputArgument = new Argument<FileSystemInfo>("input") { Description = "A scanned image, or a folder of scans." };
        var outputOption = new Option<DirectoryInfo>("--output", "-o")
        {
            Description = "Folder to save the card images in.",
            DefaultValueFactory = _ => new DirectoryInfo(DefaultOutputDirectory)
        };
        var sheetOption = new Option<FileInfo?>("--sheet", "-s")
        {
            Description = "Excel sheet to add a row per card to, with the Images column filled in. Created if it doesn't exist."
        };

        var command = new Command("split", "Cut a flatbed scan of several cards into one image per card.")
        {
            inputArgument, outputOption, sheetOption
        };
        command.SetAction(parseResult => Execute(
            parseResult.GetRequiredValue(inputArgument),
            parseResult.GetRequiredValue(outputOption),
            parseResult.GetValue(sheetOption)));

        return command;
    }

    private int Execute(FileSystemInfo input, DirectoryInfo outputDirectory, FileInfo? sheet)
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
            AddRowsToSheet(sheet, results);
        }

        return results.All(result => result.IsSuccess) ? ExitCodes.Success : ExitCodes.ValidationFailed;
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

    private void AddRowsToSheet(FileInfo sheet, IReadOnlyList<ScanSplitResult> results)
    {
        var imagePaths = results.SelectMany(result => result.Cards).Select(card => card.FilePath).ToList();

        try
        {
            if (!sheet.Exists)
            {
                templateWriter.Write(sheet.FullName);
            }

            var addedRows = sheetAppender.AppendImageRows(sheet.FullName, imagePaths);
            AnsiConsole.MarkupLineInterpolated($"[green]Added {addedRows} row(s) to[/] {sheet.FullName}");
        }
        catch (IOException)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Couldn't update {sheet.FullName}.[/] Close it in Excel and run the command again.");
        }
    }
}
