using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.DependencyInjection;
using EbaySellerTool.Core.Descriptions;
using EbaySellerTool.Core.Ebay.Inventory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Tests.TestSupport;

internal static class TestSettings
{
    public static EbayOptions Ebay(EbayEnvironment environment = EbayEnvironment.Production) => new()
    {
        Environment = environment,
        MarketplaceId = "EBAY_AU",
        Currency = "AUD",
        Locale = "en_AU"
    };

    public static ListingDefaultsOptions ListingDefaults(string descriptionTemplatePath = "") => new()
    {
        CategoryId = "183454",
        MerchantLocationKey = "home",
        FulfillmentPolicyId = "fulfillment-1",
        PaymentPolicyId = "payment-1",
        ReturnPolicyId = "return-1",
        DescriptionTemplatePath = descriptionTemplatePath
    };

    public static ListingRequestMapper CreateRequestMapper() => new(
        Options.Create(Ebay()),
        Options.Create(ListingDefaults()),
        new ListingDescriptionBuilder(Options.Create(ListingDefaults())));

    public static ServiceProvider CreateCoreServices() =>
        new ServiceCollection()
            .AddEbaySellerToolCore(new ConfigurationBuilder().Build())
            .BuildServiceProvider();
}
