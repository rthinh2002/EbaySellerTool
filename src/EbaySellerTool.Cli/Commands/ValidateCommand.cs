using System.CommandLine;
using EbaySellerTool.Cli.Rendering;
using EbaySellerTool.Core.Import;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal sealed class ValidateCommand(IListingImportService importService, ImportResultRenderer renderer) : ICliCommand
{
    public Command Build()
    {
        var fileArgument = new Argument<FileInfo>("file") { Description = "Excel sheet of cards to validate (.xlsx)." };

        var command = new Command("validate", "Check an Excel sheet for errors without listing anything on eBay.") { fileArgument };
        command.SetAction(parseResult => Execute(parseResult.GetRequiredValue(fileArgument)));

        return command;
    }

    private int Execute(FileInfo file)
    {
        if (!file.Exists)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]File not found:[/] {file.FullName}");
            return ExitCodes.InvalidInput;
        }

        if (!ExcelFiles.HasExcelExtension(file))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Expected an {ExcelFiles.Extension} file:[/] {file.FullName}");
            return ExitCodes.InvalidInput;
        }

        var result = importService.Import(file.FullName);
        renderer.Render(result);

        return result.HasErrors ? ExitCodes.ValidationFailed : ExitCodes.Success;
    }
}
