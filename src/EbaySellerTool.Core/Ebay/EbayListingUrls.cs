using EbaySellerTool.Core.Configuration;

namespace EbaySellerTool.Core.Ebay;

public static class EbayListingUrls
{
    private const string SandboxItemBaseUrl = "https://sandbox.ebay.com/itm/";

    private static readonly IReadOnlyDictionary<string, string> ProductionItemBaseUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["EBAY_AU"] = "https://www.ebay.com.au/itm/",
        ["EBAY_US"] = "https://www.ebay.com/itm/",
        ["EBAY_GB"] = "https://www.ebay.co.uk/itm/"
    };

    public static string? ForListing(string listingId, EbayOptions options)
    {
        if (options.Environment == EbayEnvironment.Sandbox)
        {
            return SandboxItemBaseUrl + listingId;
        }

        return ProductionItemBaseUrls.TryGetValue(options.MarketplaceId, out var baseUrl) ? baseUrl + listingId : null;
    }
}
