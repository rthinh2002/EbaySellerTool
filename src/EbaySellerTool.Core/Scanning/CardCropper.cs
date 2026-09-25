using OpenCvSharp;

namespace EbaySellerTool.Core.Scanning;

public sealed class CardCropper : ICardCropper
{
    public Mat Crop(Mat scan, RotatedRect cardBounds)
    {
        var corners = OrderClockwiseFromTopLeft(cardBounds.Points());

        if (Distance(corners[0], corners[1]) > Distance(corners[1], corners[2]))
        {
            corners = [corners[1], corners[2], corners[3], corners[0]];
        }

        var width = (int)Math.Round(Distance(corners[0], corners[1]));
        var height = (int)Math.Round(Distance(corners[1], corners[2]));
        Point2f[] target = [new(0, 0), new(width - 1, 0), new(width - 1, height - 1), new(0, height - 1)];

        using var transform = Cv2.GetPerspectiveTransform(corners, target);
        var card = new Mat();
        Cv2.WarpPerspective(scan, card, transform, new Size(width, height), InterpolationFlags.Cubic, BorderTypes.Replicate);

        return card;
    }

    private static Point2f[] OrderClockwiseFromTopLeft(Point2f[] points)
    {
        var topLeft = points.MinBy(point => point.X + point.Y);
        var bottomRight = points.MaxBy(point => point.X + point.Y);
        var topRight = points.MinBy(point => point.Y - point.X);
        var bottomLeft = points.MaxBy(point => point.Y - point.X);

        return [topLeft, topRight, bottomRight, bottomLeft];
    }

    private static double Distance(Point2f first, Point2f second) => Point2f.Distance(first, second);
}
