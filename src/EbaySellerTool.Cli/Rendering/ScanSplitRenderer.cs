using System.Globalization;
using EbaySellerTool.Core.Scanning;
using Spectre.Console;

namespace EbaySellerTool.Cli.Rendering;

internal sealed class ScanSplitRenderer
{
    // eBay recommends at least 1600px on the longest side so buyers can zoom in.
    private const int RecommendedLongestSide = 1600;

    public void Render(IReadOnlyList<ScanSplitResult> results, string outputDirectory)
    {
        var table = new Table().AddColumns("Scan", "Cards", "Details");

        foreach (var result in results)
        {
            table.AddRow(
                new Text(Path.GetFileName(result.ScanPath)),
                new Markup(result.IsSuccess ? $"[green]{result.Cards.Count}[/]" : "[red]0[/]"),
                new Text(result.Error ?? DescribeCardSize(result.Cards)));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLineInterpolated($"Saved {results.Sum(result => result.Cards.Count)} card image(s) to {outputDirectory}");
        WarnAboutLowResolution(results);
    }

    private static string DescribeCardSize(IReadOnlyList<CardImage> cards)
    {
        var smallest = cards.MinBy(card => card.LongestSide)!;
        return string.Create(CultureInfo.InvariantCulture, $"Smallest card: {smallest.Width} x {smallest.Height} px");
    }

    private static void WarnAboutLowResolution(IReadOnlyList<ScanSplitResult> results)
    {
        var lowResolutionCount = results.SelectMany(result => result.Cards).Count(card => card.LongestSide < RecommendedLongestSide);

        if (lowResolutionCount > 0)
        {
            AnsiConsole.MarkupLineInterpolated(
                $"[yellow]{lowResolutionCount} image(s) are under {RecommendedLongestSide}px on the longest side. Scan at 600 DPI for sharper eBay photos.[/]");
        }
    }
}
