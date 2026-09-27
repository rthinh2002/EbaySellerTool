namespace EbaySellerTool.Core.Ebay.Auth;

/// <summary>
/// The page eBay sends the user to after they click "Agree". It carries the one-time authorization code
/// (and our state value) as URL-encoded query parameters.
/// </summary>
public sealed record AuthorizationRedirect(string Code, string? State)
{
    public static bool TryParse(string? redirectUrl, out AuthorizationRedirect? redirect)
    {
        redirect = null;

        if (!Uri.TryCreate(redirectUrl?.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var parameters = ParseQuery(uri.Query);

        if (!parameters.TryGetValue("code", out var code) || code.Length == 0)
        {
            return false;
        }

        redirect = new AuthorizationRedirect(code, parameters.GetValueOrDefault("state"));
        return true;
    }

    private static Dictionary<string, string> ParseQuery(string query) =>
        query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .GroupBy(parts => Uri.UnescapeDataString(parts[0]), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => Uri.UnescapeDataString(group.First()[1]), StringComparer.OrdinalIgnoreCase);
}
