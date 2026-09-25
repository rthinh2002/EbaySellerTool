using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Validation;

namespace EbaySellerTool.Core.Listing;

public static class ListingOutcomes
{
    public static IEnumerable<ListingOutcome> ForInvalidRows(IEnumerable<ValidationError> errors) =>
        errors
            .Where(error => error.RowNumber is not null)
            .GroupBy(error => error.RowNumber!.Value)
            .Select(row => new ListingOutcome
            {
                RowNumber = row.Key,
                Status = ListingStatus.Invalid,
                Errors = [.. row.Select(FormatValidationError)]
            });

    public static IEnumerable<ListingOutcome> ForDryRun(IEnumerable<CardListing> listings) =>
        listings.Select(listing => new ListingOutcome
        {
            RowNumber = listing.RowNumber,
            Sku = listing.Sku,
            Title = listing.Title,
            Status = ListingStatus.DryRun
        });

    private static string FormatValidationError(ValidationError error) =>
        error.Column is null ? error.Message : $"{error.Column}: {error.Message}";
}
