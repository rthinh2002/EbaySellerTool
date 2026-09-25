using OpenCvSharp;

namespace EbaySellerTool.Core.Scanning;

/// <summary>
/// Finds cards on a flatbed scan by separating them from the bright, unsaturated scanner background,
/// then keeping the blobs that are big enough and shaped like a trading card.
/// </summary>
public sealed class CardDetector : ICardDetector
{
    private const double BackgroundBrightness = 200;
    private const double BackgroundMaxSaturation = 40;
    private const double MinCardAreaFraction = 0.01;
    private const double StandardCardAspectRatio = 88.0 / 63.0;
    private const double AspectRatioTolerance = 0.2;
    private const double MinRectangularity = 0.85;
    private const double GapClosingKernelFraction = 0.005;
    private const double ThinLineKernelFraction = 0.015;
    private const int MinKernelSize = 3;

    public IReadOnlyList<RotatedRect> Detect(Mat scan)
    {
        using var foreground = CreateForegroundMask(scan);
        Cv2.FindContours(foreground, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        var minCardArea = scan.Width * scan.Height * MinCardAreaFraction;

        return
        [
            .. contours
                .Select(contour => (Area: Cv2.ContourArea(contour), Bounds: Cv2.MinAreaRect(contour)))
                .Where(candidate => candidate.Area >= minCardArea && IsCardShaped(candidate.Bounds, candidate.Area))
                .Select(candidate => candidate.Bounds)
        ];
    }

    private static Mat CreateForegroundMask(Mat scan)
    {
        using var gray = new Mat();
        using var hsv = new Mat();
        using var darkPixels = new Mat();
        using var saturatedPixels = new Mat();

        Cv2.CvtColor(scan, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.GaussianBlur(gray, gray, new Size(5, 5), 0);
        Cv2.Threshold(gray, darkPixels, BackgroundBrightness, 255, ThresholdTypes.BinaryInv);

        // Saturation catches pale but colourful card borders that are almost as bright as the background.
        Cv2.CvtColor(scan, hsv, ColorConversionCodes.BGR2HSV);
        using var saturation = hsv.ExtractChannel(1);
        Cv2.Threshold(saturation, saturatedPixels, BackgroundMaxSaturation, 255, ThresholdTypes.Binary);

        var foreground = new Mat();
        Cv2.BitwiseOr(darkPixels, saturatedPixels, foreground);

        var shortSide = Math.Min(scan.Width, scan.Height);
        ApplyMorphology(foreground, MorphTypes.Close, KernelSize(shortSide, GapClosingKernelFraction));

        // Opening removes the thin shadow lines scanners leave along the glass edges, which would otherwise
        // join any card touching them into one oddly shaped blob.
        ApplyMorphology(foreground, MorphTypes.Open, KernelSize(shortSide, ThinLineKernelFraction));

        return foreground;
    }

    private static void ApplyMorphology(Mat mask, MorphTypes operation, int kernelSize)
    {
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(kernelSize, kernelSize));
        Cv2.MorphologyEx(mask, mask, operation, kernel);
    }

    private static int KernelSize(int imageShortSide, double fraction) =>
        Math.Max(MinKernelSize, (int)Math.Round(imageShortSide * fraction));

    private static bool IsCardShaped(RotatedRect bounds, double contourArea)
    {
        var longSide = Math.Max(bounds.Size.Width, bounds.Size.Height);
        var shortSide = Math.Min(bounds.Size.Width, bounds.Size.Height);

        if (shortSide <= 0)
        {
            return false;
        }

        var aspectRatio = longSide / shortSide;
        var rectangularity = contourArea / (longSide * shortSide);

        return Math.Abs(aspectRatio - StandardCardAspectRatio) <= AspectRatioTolerance && rectangularity >= MinRectangularity;
    }
}
