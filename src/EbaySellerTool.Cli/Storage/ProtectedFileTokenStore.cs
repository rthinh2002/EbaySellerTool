using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay.Auth;

namespace EbaySellerTool.Cli.Storage;

/// <summary>
/// Keeps tokens in %LOCALAPPDATA%\EbaySellerTool, encrypted with Windows DPAPI so only the current
/// Windows user can read them.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class ProtectedFileTokenStore : ITokenStore
{
    public async Task<EbayToken?> LoadAsync(EbayEnvironment environment, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath(environment);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var encrypted = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var json = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);

        return JsonSerializer.Deserialize<EbayToken>(json);
    }

    public async Task SaveAsync(EbayEnvironment environment, EbayToken token, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(token);
        var encrypted = ProtectedData.Protect(json, optionalEntropy: null, DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(GetFilePath(environment), encrypted, cancellationToken);
    }

    private static string GetFilePath(EbayEnvironment environment) =>
        AppDataPaths.GetFilePath($"token.{environment.ToString().ToLowerInvariant()}.bin");
}
