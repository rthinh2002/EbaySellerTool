using System.Text.Json;
using System.Text.Json.Nodes;
using EbaySellerTool.Core.Configuration;

namespace EbaySellerTool.Cli.Storage;

/// <summary>
/// Per-environment settings written by <c>ebaytool setup</c> (policy IDs, location), stored in
/// %LOCALAPPDATA%\EbaySellerTool and layered over appsettings.json.
/// </summary>
internal static class UserSettingsFile
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static string PathFor(EbayEnvironment environment) =>
        AppDataPaths.GetFilePath($"settings.{environment.ToString().ToLowerInvariant()}.json");

    public static void SaveListingDefaults(EbayEnvironment environment, IReadOnlyDictionary<string, string> values)
    {
        var filePath = PathFor(environment);
        var root = File.Exists(filePath) ? JsonNode.Parse(File.ReadAllText(filePath))?.AsObject() ?? [] : new JsonObject();

        if (root[ListingDefaultsOptions.SectionName] is not JsonObject listingDefaults)
        {
            listingDefaults = [];
            root[ListingDefaultsOptions.SectionName] = listingDefaults;
        }

        foreach (var (name, value) in values)
        {
            listingDefaults[name] = value;
        }

        File.WriteAllText(filePath, root.ToJsonString(WriteOptions));
    }
}
