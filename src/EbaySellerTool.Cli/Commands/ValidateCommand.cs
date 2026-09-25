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
        if (ExcelFiles.FindInputFileProblem(file) is { } problem)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{problem}[/]");
            return ExitCodes.InvalidInput;
        }

        var result = importService.Import(file.FullName);
        renderer.Render(result);

        return result.HasErrors ? ExitCodes.ValidationFailed : ExitCodes.Success;
    }
}
