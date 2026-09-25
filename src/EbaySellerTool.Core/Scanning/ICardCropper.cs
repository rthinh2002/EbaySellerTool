using OpenCvSharp;

namespace EbaySellerTool.Core.Scanning;

public interface ICardCropper
{
    /// <summary>Cuts the card out of the scan, straightened and in portrait orientation.</summary>
    Mat Crop(Mat scan, RotatedRect cardBounds);
}
