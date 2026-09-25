using OpenCvSharp;

namespace EbaySellerTool.Core.Scanning;

public sealed class ScanSplitter(ICardDetector cardDetector, ICardCropper cardCropper) : IScanSplitter
{
    private const int JpegQuality = 95;

    public static IReadOnlySet<string> SupportedScanExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff"
    };

    public ScanSplitResult Split(string scanPath, string outputDirectory)
    {
        using var scan = Cv2.ImRead(scanPath, ImreadModes.Color);

        if (scan.Empty())
        {
            return ScanSplitResult.Failure(scanPath, "Could not read the image.");
        }

        var cardBounds = CardReadingOrder.Sort(cardDetector.Detect(scan));

        if (cardBounds.Count == 0)
        {
            return ScanSplitResult.Failure(scanPath, "No cards found. Leave a gap between cards and keep them inside the scan area.");
        }

        Directory.CreateDirectory(outputDirectory);
        var scanName = Path.GetFileNameWithoutExtension(scanPath);

        var cards = cardBounds
            .Select((bounds, index) => SaveCard(scan, bounds, Path.Combine(outputDirectory, $"{scanName}_card{index + 1:00}.jpg")))
            .ToList();

        return new ScanSplitResult(scanPath, cards);
    }

    private CardImage SaveCard(Mat scan, RotatedRect bounds, string filePath)
    {
        using var card = cardCropper.Crop(scan, bounds);
        Cv2.ImWrite(filePath, card, new ImageEncodingParam(ImwriteFlags.JpegQuality, JpegQuality));

        return new CardImage(filePath, card.Width, card.Height);
    }
}
