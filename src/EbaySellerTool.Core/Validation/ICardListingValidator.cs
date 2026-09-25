using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Core.Validation;

public interface ICardListingValidator
{
    IReadOnlyList<ValidationError> Validate(IReadOnlyList<CardListing> listings);
}
