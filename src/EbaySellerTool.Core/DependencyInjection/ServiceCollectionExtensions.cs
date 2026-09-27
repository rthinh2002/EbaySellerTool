using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Descriptions;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Inventory;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Import;
using EbaySellerTool.Core.Listing.DryRun;
using EbaySellerTool.Core.Reporting;
using EbaySellerTool.Core.Scanning;
using EbaySellerTool.Core.Validation;
using EbaySellerTool.Core.Validation.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EbaySellerTool.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEbaySellerToolCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EbayOptions>(configuration.GetSection(EbayOptions.SectionName));
        services.Configure<ListingDefaultsOptions>(configuration.GetSection(ListingDefaultsOptions.SectionName));

        services.AddSingleton<IListingSheetReader, ListingSheetReader>();
        services.AddSingleton<IListingTemplateWriter, ListingTemplateWriter>();
        services.AddSingleton<ICardListingParser, CardListingParser>();
        services.AddSingleton<IListingImportService, ListingImportService>();
        services.AddListingValidation();

        services.AddSingleton<IListingDescriptionBuilder, ListingDescriptionBuilder>();
        services.AddSingleton<IListingRequestMapper, ListingRequestMapper>();
        services.AddSingleton<IDryRunPlanner, DryRunPlanner>();
        services.AddSingleton<IListingReportWriter, ListingReportWriter>();

        services.AddSingleton<IListingSheetAppender, ListingSheetAppender>();
        services.AddSingleton<ICardDetector, CardDetector>();
        services.AddSingleton<ICardCropper, CardCropper>();
        services.AddSingleton<IScanSplitter, ScanSplitter>();

        services.AddEbayAuth();

        return services;
    }

    /// <remarks>The host must also register an <see cref="ITokenStore"/>.</remarks>
    private static void AddEbayAuth(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient(EbayOAuthClient.HttpClientName);
        services.AddSingleton<IEbayOAuthClient, EbayOAuthClient>();
        services.AddSingleton<IAccessTokenProvider, AccessTokenProvider>();
    }

    private static void AddListingValidation(this IServiceCollection services)
    {
        services.AddSingleton<ICardListingValidator, CardListingValidator>();

        services.AddSingleton<IListingRule, TitleRule>();
        services.AddSingleton<IListingRule, SkuRule>();
        services.AddSingleton<IListingRule, PriceRule>();
        services.AddSingleton<IListingRule, QuantityRule>();
        services.AddSingleton<IListingRule, ImagesRule>();
        services.AddSingleton<IListingRule, CategoryIdRule>();

        services.AddSingleton<IListingBatchRule, DuplicateSkuRule>();
    }
}
