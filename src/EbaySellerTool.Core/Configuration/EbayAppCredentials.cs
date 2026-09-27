namespace EbaySellerTool.Core.Configuration;

public sealed class EbayAppCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>eBay's "redirect URL name" for the app, sent as the OAuth <c>redirect_uri</c>.</summary>
    public string RuName { get; set; } = string.Empty;

    public IReadOnlyList<string> GetMissingSettings(EbayEnvironment environment)
    {
        (string Name, string Value)[] settings = [(nameof(ClientId), ClientId), (nameof(ClientSecret), ClientSecret), (nameof(RuName), RuName)];

        return [.. settings
            .Where(setting => string.IsNullOrWhiteSpace(setting.Value))
            .Select(setting => $"{EbayOptions.SectionName}:{environment}:{setting.Name}")];
    }
}
