using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Core.Validation;

public interface IListingBatchRule
{
    IEnumerable<ValidationError> Validate(IReadOnlyList<CardListing> listings);
}
