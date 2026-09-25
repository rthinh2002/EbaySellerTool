namespace EbaySellerTool.Core.Scanning;

public sealed record ScanSplitResult(string ScanPath, IReadOnlyList<CardImage> Cards, string? Error = null)
{
    public bool IsSuccess => Error is null && Cards.Count > 0;

    public static ScanSplitResult Failure(string scanPath, string error) => new(scanPath, [], error);
}
