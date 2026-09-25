namespace EbaySellerTool.Cli.Commands;

internal static class ExcelFiles
{
    public const string Extension = ".xlsx";

    public static bool HasExcelExtension(FileInfo file) =>
        string.Equals(file.Extension, Extension, StringComparison.OrdinalIgnoreCase);
}
