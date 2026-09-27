namespace EbaySellerTool.Cli.Commands;

/// <summary>
/// Default folder layout of a listing workspace (the folder commands are run from, e.g. <c>samples/</c>).
/// </summary>
internal static class WorkspaceFolders
{
    public const string Sheets = "sheets";
    public const string Scans = "scans";
    public const string Images = "images";
    public const string Results = "results";
    public const string DryRuns = "dry-runs";

    /// <summary>A timestamped output file, e.g. <c>results/results_cards_20260927_210000.xlsx</c>; the folder is created if needed.</summary>
    public static string OutputFilePath(string folder, FileInfo inputFile, string prefix, DateTime timestamp, string extension)
    {
        var directory = Directory.CreateDirectory(folder);
        var fileName = $"{prefix}_{Path.GetFileNameWithoutExtension(inputFile.Name)}_{timestamp:yyyyMMdd_HHmmss}{extension}";

        return Path.Combine(directory.FullName, fileName);
    }
}
