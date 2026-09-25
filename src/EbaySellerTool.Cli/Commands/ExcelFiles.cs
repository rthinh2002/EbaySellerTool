namespace EbaySellerTool.Cli.Commands;

internal static class ExcelFiles
{
    public const string Extension = ".xlsx";

    public static bool HasExcelExtension(FileInfo file) =>
        string.Equals(file.Extension, Extension, StringComparison.OrdinalIgnoreCase);

    /// <returns>A problem description, or null when the file can be imported.</returns>
    public static string? FindInputFileProblem(FileInfo file)
    {
        if (!file.Exists)
        {
            return $"File not found: {file.FullName}";
        }

        return HasExcelExtension(file) ? null : $"Expected an {Extension} file: {file.FullName}";
    }

    public static string OutputPathNextTo(FileInfo inputFile, string prefix, DateTime timestamp, string extension) =>
        Path.Combine(
            inputFile.DirectoryName!,
            $"{prefix}_{Path.GetFileNameWithoutExtension(inputFile.Name)}_{timestamp:yyyyMMdd_HHmmss}{extension}");
}
