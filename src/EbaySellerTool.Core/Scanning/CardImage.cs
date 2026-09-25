namespace EbaySellerTool.Core.Scanning;

public sealed record CardImage(string FilePath, int Width, int Height)
{
    public int LongestSide => Math.Max(Width, Height);
}
