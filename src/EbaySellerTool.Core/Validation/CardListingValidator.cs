using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Core.Validation;

public sealed class CardListingValidator(
    IEnumerable<IListingRule> listingRules,
    IEnumerable<IListingBatchRule> batchRules) : ICardListingValidator
{
    public IReadOnlyList<ValidationError> Validate(IReadOnlyList<CardListing> listings)
    {
        var listingErrors = listings.SelectMany(listing => listingRules.SelectMany(rule => rule.Validate(listing)));
        var batchErrors = batchRules.SelectMany(rule => rule.Validate(listings));

        return [.. listingErrors.Concat(batchErrors).OrderBy(error => error.RowNumber)];
    }
}
