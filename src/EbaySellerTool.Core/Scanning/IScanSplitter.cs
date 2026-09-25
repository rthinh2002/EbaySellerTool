namespace EbaySellerTool.Core.Scanning;

public interface IScanSplitter
{
    /// <summary>Saves each card found on the scan as its own image, numbered in reading order.</summary>
    ScanSplitResult Split(string scanPath, string outputDirectory);
}
