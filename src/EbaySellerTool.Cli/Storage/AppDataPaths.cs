namespace EbaySellerTool.Cli.Storage;

internal static class AppDataPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EbaySellerTool");

    public static string GetFilePath(string fileName)
    {
        Directory.CreateDirectory(Root);
        return Path.Combine(Root, fileName);
    }
}
