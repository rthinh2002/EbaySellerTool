using OpenCvSharp;

namespace EbaySellerTool.Core.Scanning;

/// <summary>Sorts detected cards left-to-right, top-to-bottom, the way the grid is laid out on the scanner.</summary>
public static class CardReadingOrder
{
    public static IReadOnlyList<RotatedRect> Sort(IReadOnlyList<RotatedRect> cards)
    {
        if (cards.Count == 0)
        {
            return [];
        }

        var rowTolerance = cards.Select(ShortSide).Order().ElementAt(cards.Count / 2) / 2;
        var rows = new List<List<RotatedRect>>();

        foreach (var card in cards.OrderBy(card => card.Center.Y))
        {
            var currentRow = rows.LastOrDefault();

            if (currentRow is not null && card.Center.Y - currentRow[0].Center.Y <= rowTolerance)
            {
                currentRow.Add(card);
            }
            else
            {
                rows.Add([card]);
            }
        }

        return [.. rows.SelectMany(row => row.OrderBy(card => card.Center.X))];
    }

    private static float ShortSide(RotatedRect card) => Math.Min(card.Size.Width, card.Size.Height);
}
