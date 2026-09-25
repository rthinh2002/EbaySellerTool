using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Core.Validation;

public interface IListingRule
{
    IEnumerable<ValidationError> Validate(CardListing listing);
}
