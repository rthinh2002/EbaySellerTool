using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Descriptions;
using EbaySellerTool.Core.Ebay.Account;
using EbaySellerTool.Core.Ebay.Auth;
using EbaySellerTool.Core.Ebay.Http;
using EbaySellerTool.Core.Ebay.Inventory;
using EbaySellerTool.Core.Ebay.Media;
using EbaySellerTool.Core.Ebay.Stores;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Import;
using EbaySellerTool.Core.Listing;
using EbaySellerTool.Core.Listing.DryRun;
using EbaySellerTool.Core.Listing.Steps;
using EbaySellerTool.Core.Reporting;
using EbaySellerTool.Core.Scanning;
using EbaySellerTool.Core.Validation;
using EbaySellerTool.Core.Validation.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private static readonly TimeSpan HttpAttemptTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan HttpTotalTimeout = TimeSpan.FromMinutes(3);

    /// <remarks>
    /// The host must also register an <see cref="ITokenStore"/>, an <see cref="IImageUrlCache"/> and an <see cref="IStoreCategoryCache"/>.
    /// </remarks>
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
        services.AddEbayApiClients();
        services.AddListingPipeline();

        return services;
    }

    private static void AddEbayAuth(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient(EbayOAuthClient.HttpClientName);
        services.AddSingleton<IEbayOAuthClient, EbayOAuthClient>();
        services.AddSingleton<IAccessTokenProvider, AccessTokenProvider>();
    }

    private static void AddEbayApiClients(this IServiceCollection services)
    {
        services.AddHttpClient(EbayRestClient.HttpClientName)
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.AttemptTimeout.Timeout = HttpAttemptTimeout;
                resilience.TotalRequestTimeout.Timeout = HttpTotalTimeout;
                resilience.CircuitBreaker.SamplingDuration = HttpAttemptTimeout * 2;
            });

        services.AddSingleton<EbayRestClient>();
        services.AddSingleton<IEbayAccountClient, EbayAccountClient>();
        services.AddSingleton<IEbayLocationClient, EbayLocationClient>();
        services.AddSingleton<IEbayInventoryClient, EbayInventoryClient>();
        services.AddSingleton<IEbayStoreClient, EbayStoreClient>();
        services.AddSingleton<IStoreCategoryCatalog, StoreCategoryCatalog>();

        services.AddSingleton<EbayMediaImageUploader>();
        services.AddSingleton<SandboxPlaceholderImageUploader>();
        services.AddSingleton<IImageUploader>(CreateImageUploader);
    }

    private static IImageUploader CreateImageUploader(IServiceProvider services)
    {
        var environment = services.GetRequiredService<IOptions<EbayOptions>>().Value.Environment;

        return environment == EbayEnvironment.Sandbox
            ? services.GetRequiredService<SandboxPlaceholderImageUploader>()
            : new CachingImageUploader(
                services.GetRequiredService<EbayMediaImageUploader>(),
                services.GetRequiredService<IImageUrlCache>(),
                services.GetRequiredService<TimeProvider>());
    }

    private static void AddListingPipeline(this IServiceCollection services)
    {
        // Registration order is the order ListingService runs the steps in.
        services.AddSingleton<IListingStep, ImageUploadStep>();
        services.AddSingleton<IListingStep, InventoryItemStep>();
        services.AddSingleton<IListingStep, OfferStep>();
        services.AddSingleton<IListingStep, PublishStep>();

        services.AddSingleton<IListingService, ListingService>();
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
        services.AddSingleton<IListingRule, StoreCategoryRule>();

        services.AddSingleton<IListingBatchRule, DuplicateSkuRule>();
    }
}
