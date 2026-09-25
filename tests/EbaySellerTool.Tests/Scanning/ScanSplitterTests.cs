using EbaySellerTool.Core.Scanning;
using EbaySellerTool.Tests.TestSupport;
using OpenCvSharp;

namespace EbaySellerTool.Tests.Scanning;

public class ScanSplitterTests
{
    private static readonly string RealScanPath = Path.Combine(AppContext.BaseDirectory, "TestData", "riftbound_scan.jpg");

    private readonly ScanSplitter _splitter = new(new CardDetector(), new CardCropper());

    [Fact]
    public void Split_RealScanOnWhiteBackground_FindsAllNineCards()
    {
        using var directory = new TemporaryDirectory();

        var result = _splitter.Split(RealScanPath, directory.Path);

        Assert.True(result.IsSuccess);
        Assert.Equal(9, result.Cards.Count);
        Assert.All(result.Cards, card =>
        {
            Assert.True(File.Exists(card.FilePath));
            Assert.InRange((double)card.Height / card.Width, 1.3, 1.5);
        });
    }

    [Fact]
    public void Split_NamesImagesAfterScanInReadingOrder()
    {
        using var directory = new TemporaryDirectory();
        var scanPath = directory.GetFilePath("scan 1.png");
        using (var scan = SyntheticScan.WithCards(columns: 2, rows: 2))
        {
            Cv2.ImWrite(scanPath, scan);
        }

        var result = _splitter.Split(scanPath, directory.GetFilePath("out"));

        Assert.Equal(
            ["scan 1_card01.jpg", "scan 1_card02.jpg", "scan 1_card03.jpg", "scan 1_card04.jpg"],
            result.Cards.Select(card => Path.GetFileName(card.FilePath)));
    }

    [Fact]
    public void Split_RotatedCard_IsStraightenedToPortrait()
    {
        using var directory = new TemporaryDirectory();
        var scanPath = directory.GetFilePath("rotated.png");
        using (var scan = SyntheticScan.WithSingleCard(angle: 7))
        {
            Cv2.ImWrite(scanPath, scan);
        }

        var card = Assert.Single(_splitter.Split(scanPath, directory.Path).Cards);

        Assert.InRange(card.Width, 245, 255);
        Assert.InRange(card.Height, 345, 355);
    }

    [Fact]
    public void Split_BlankScan_ReturnsNoCardsError()
    {
        using var directory = new TemporaryDirectory();
        var scanPath = directory.GetFilePath("blank.png");
        using (var blank = new Mat(1000, 800, MatType.CV_8UC3, Scalar.White))
        {
            Cv2.ImWrite(scanPath, blank);
        }

        var result = _splitter.Split(scanPath, directory.Path);

        Assert.False(result.IsSuccess);
        Assert.StartsWith("No cards found", result.Error);
    }

    [Fact]
    public void Split_UnreadableFile_ReturnsError()
    {
        using var directory = new TemporaryDirectory();
        var scanPath = directory.CreateFile("broken.jpg");

        var result = _splitter.Split(scanPath, directory.Path);

        Assert.Equal("Could not read the image.", result.Error);
    }
}
