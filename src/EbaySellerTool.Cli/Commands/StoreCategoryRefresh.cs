using EbaySellerTool.Core.Ebay;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Stores;
using Spectre.Console;

namespace EbaySellerTool.Cli.Commands;

internal static class StoreCategoryRefresh
{
    /// <summary>
    /// Refreshes the saved store categories as a side step of another command. Failing is fine:
    /// validation then uses the last saved copy.
    /// </summary>
    public static async Task TryRefreshAsync(IStoreCategoryCatalog catalog, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await catalog.RefreshAsync(cancellationToken);
            AnsiConsole.MarkupLineInterpolated($"Loaded {categories.Count} store categories.");
        }
        catch (Exception exception) when (exception is EbayNotSignedInException or EbayApiException)
        {
            AnsiConsole.MarkupLineInterpolated($"[yellow]Couldn't refresh store categories ({exception.Message}); using the last saved copy.[/]");
        }
    }
}
