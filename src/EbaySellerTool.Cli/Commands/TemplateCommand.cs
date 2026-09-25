using System.CommandLine;
using EbaySellerTool.Core.Excel;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal sealed class TemplateCommand(IListingTemplateWriter templateWriter) : ICliCommand
{
    public Command Build()
    {
        var fileArgument = new Argument<FileInfo>("file") { Description = "Path of the Excel template to create (.xlsx)." };
        var forceOption = new Option<bool>("--force", "-f") { Description = "Overwrite the file if it already exists." };

        var command = new Command("template", "Create a blank Excel sheet for card listings.") { fileArgument, forceOption };
        command.SetAction(parseResult => Execute(parseResult.GetRequiredValue(fileArgument), parseResult.GetValue(forceOption)));

        return command;
    }

    private int Execute(FileInfo file, bool overwrite)
    {
        if (!ExcelFiles.HasExcelExtension(file))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]The template must be an {ExcelFiles.Extension} file.[/]");
            return ExitCodes.InvalidInput;
        }

        if (file.Exists && !overwrite)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{file.FullName} already exists.[/] Use --force to overwrite it.");
            return ExitCodes.InvalidInput;
        }

        templateWriter.Write(file.FullName);
        AnsiConsole.MarkupLineInterpolated($"[green]Template created:[/] {file.FullName}");

        return ExitCodes.Success;
    }
}
