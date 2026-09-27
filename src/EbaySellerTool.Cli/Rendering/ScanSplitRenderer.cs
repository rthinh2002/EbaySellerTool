using System.Globalization;
using EbaySellerTool.Core.Recognition;
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

    public void RenderRecognitions(IReadOnlyList<(string ImagePath, CardRecognitionResult Result)> recognitions)
    {
        var table = new Table().AddColumns("Image", "Card", "Number", "Set", "Rarity");

        foreach (var (imagePath, result) in recognitions)
        {
            var card = result.Card;
            table.AddRow(
                new Text(Path.GetFileName(imagePath)),
                result.IsSuccess ? new Text(card!.CardName ?? "?") : new Markup($"[red]{Markup.Escape(result.Error)}[/]"),
                new Text(card?.CardNumber ?? string.Empty),
                new Text(card?.SetName ?? string.Empty),
                new Text(card?.Rarity ?? string.Empty));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[yellow]Check the details Claude read before listing; it can misread cards.[/]");
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
