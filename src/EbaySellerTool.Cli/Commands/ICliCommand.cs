using System.CommandLine;

namespace EbaySellerTool.Cli.Commands;

internal interface ICliCommand
{
    Command Build();
}
