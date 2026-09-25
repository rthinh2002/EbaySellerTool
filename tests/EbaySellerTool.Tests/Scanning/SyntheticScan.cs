using OpenCvSharp;

namespace EbaySellerTool.Tests.Scanning;

/// <summary>Draws dark 250 x 350 px "cards" on a white page, standing in for a flatbed scan.</summary>
internal static class SyntheticScan
{
    private const int CardWidth = 250;
    private const int CardHeight = 350;
    private const int Gap = 60;

    public static Mat WithCards(int columns, int rows)
    {
        var scan = new Mat(Gap + rows * (CardHeight + Gap), Gap + columns * (CardWidth + Gap), MatType.CV_8UC3, Scalar.White);

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var topLeft = new Point(Gap + column * (CardWidth + Gap), Gap + row * (CardHeight + Gap));
                Cv2.Rectangle(scan, new Rect(topLeft, new Size(CardWidth, CardHeight)), new Scalar(40, 30, 30), thickness: -1);
            }
        }

        return scan;
    }

    public static Mat WithSingleCard(float angle)
    {
        var scan = new Mat(800, 700, MatType.CV_8UC3, Scalar.White);
        var card = new RotatedRect(new Point2f(350, 400), new Size2f(CardWidth, CardHeight), angle);
        var corners = card.Points().Select(point => new Point((int)Math.Round(point.X), (int)Math.Round(point.Y)));

        Cv2.FillConvexPoly(scan, corners, new Scalar(40, 30, 30));
        return scan;
    }
}
