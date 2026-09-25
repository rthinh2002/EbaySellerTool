using EbaySellerTool.Core.Scanning;
using OpenCvSharp;

namespace EbaySellerTool.Tests.Scanning;

public class CardReadingOrderTests
{
    [Fact]
    public void Sort_SlightlyUnevenGrid_OrdersRowByRowLeftToRight()
    {
        RotatedRect[] cards =
        [
            Card(x: 700, y: 1050),
            Card(x: 250, y: 380),
            Card(x: 1200, y: 360),
            Card(x: 250, y: 1020),
            Card(x: 720, y: 400)
        ];

        var sorted = CardReadingOrder.Sort(cards);

        Assert.Equal(
            [(250f, 380f), (720f, 400f), (1200f, 360f), (250f, 1020f), (700f, 1050f)],
            sorted.Select(card => (card.Center.X, card.Center.Y)));
    }

    [Fact]
    public void Sort_NoCards_ReturnsEmpty()
    {
        Assert.Empty(CardReadingOrder.Sort([]));
    }

    private static RotatedRect Card(float x, float y) => new(new Point2f(x, y), new Size2f(425, 597), 0);
}
