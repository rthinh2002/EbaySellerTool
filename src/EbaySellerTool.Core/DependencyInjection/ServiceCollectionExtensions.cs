using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Import;
using EbaySellerTool.Core.Validation;
using EbaySellerTool.Core.Validation.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace EbaySellerTool.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEbaySellerToolCore(this IServiceCollection services)
    {
        services.AddSingleton<IListingSheetReader, ListingSheetReader>();
        services.AddSingleton<IListingTemplateWriter, ListingTemplateWriter>();
        services.AddSingleton<ICardListingParser, CardListingParser>();
        services.AddSingleton<IListingImportService, ListingImportService>();
        services.AddListingValidation();

        return services;
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
