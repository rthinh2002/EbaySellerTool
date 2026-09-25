namespace EbaySellerTool.Core.Configuration;

public sealed class EbayOptions
{
    public const string SectionName = "Ebay";

    public EbayEnvironment Environment { get; set; } = EbayEnvironment.Sandbox;
    public string MarketplaceId { get; set; } = "EBAY_AU";
    public string Currency { get; set; } = "AUD";
    public string Locale { get; set; } = "en_AU";
}
