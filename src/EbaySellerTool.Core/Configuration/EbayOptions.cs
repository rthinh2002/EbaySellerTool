namespace EbaySellerTool.Core.Configuration;

public sealed class EbayOptions
{
    public const string SectionName = "Ebay";

    public EbayEnvironment Environment { get; set; } = EbayEnvironment.Sandbox;
    public string MarketplaceId { get; set; } = "EBAY_AU";
    public string Currency { get; set; } = "AUD";
    public string Locale { get; set; } = "en_AU";

    /// <summary>Public image used for every photo in the Sandbox, where eBay's image upload isn't available.</summary>
    public string SandboxPlaceholderImageUrl { get; set; } =
        "https://raw.githubusercontent.com/rthinh2002/EbaySellerTool/main/samples/images/riftbound_scan_card01.jpg";

    /// <summary>Stored in user-secrets as <c>Ebay:Sandbox:ClientId</c> and so on.</summary>
    public EbayAppCredentials Sandbox { get; set; } = new();

    public EbayAppCredentials Production { get; set; } = new();

    public EbayAppCredentials ActiveCredentials => Environment == EbayEnvironment.Sandbox ? Sandbox : Production;

    /// <summary>The <c>Content-Language</c> header value eBay expects, e.g. "en-AU" for locale "en_AU".</summary>
    public string ContentLanguage => Locale.Replace('_', '-');
}
