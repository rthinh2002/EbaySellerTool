namespace EbaySellerTool.Core.Ebay.Auth;

public static class EbayScopes
{
    public const string Base = "https://api.ebay.com/oauth/api_scope";
    public const string SellInventory = "https://api.ebay.com/oauth/api_scope/sell.inventory";
    public const string SellAccount = "https://api.ebay.com/oauth/api_scope/sell.account";

    /// <summary>Everything the tool needs. The Media API also uses <see cref="SellInventory"/>.</summary>
    public static IReadOnlyList<string> All { get; } = [Base, SellInventory, SellAccount];

    public static string ToScopeParameter(IEnumerable<string> scopes) => string.Join(' ', scopes);
}
