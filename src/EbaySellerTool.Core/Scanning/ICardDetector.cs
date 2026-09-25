using OpenCvSharp;

namespace EbaySellerTool.Core.Scanning;

public interface ICardDetector
{
    IReadOnlyList<RotatedRect> Detect(Mat scan);
}
