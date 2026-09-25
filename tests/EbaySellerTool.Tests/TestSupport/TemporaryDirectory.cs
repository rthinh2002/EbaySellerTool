namespace EbaySellerTool.Tests.TestSupport;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"EbaySellerTool.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string GetFilePath(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    public string CreateFile(string relativePath)
    {
        var filePath = GetFilePath(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath)!);
        File.WriteAllBytes(filePath, []);
        return filePath;
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
