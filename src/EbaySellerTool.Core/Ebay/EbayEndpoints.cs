using EbaySellerTool.Core.Configuration;

namespace EbaySellerTool.Core.Ebay;

public static class EbayEndpoints
{
    public static Uri AuthorizeUrl(EbayEnvironment environment) => environment == EbayEnvironment.Sandbox
        ? new Uri("https://auth.sandbox.ebay.com/oauth2/authorize")
        : new Uri("https://auth.ebay.com/oauth2/authorize");

    public static Uri ApiBaseUrl(EbayEnvironment environment) => environment == EbayEnvironment.Sandbox
        ? new Uri("https://api.sandbox.ebay.com/")
        : new Uri("https://api.ebay.com/");

    public static Uri MediaBaseUrl(EbayEnvironment environment) => environment == EbayEnvironment.Sandbox
        ? new Uri("https://apim.sandbox.ebay.com/")
        : new Uri("https://apim.ebay.com/");

    public static Uri TokenUrl(EbayEnvironment environment) => new(ApiBaseUrl(environment), "identity/v1/oauth2/token");
}
