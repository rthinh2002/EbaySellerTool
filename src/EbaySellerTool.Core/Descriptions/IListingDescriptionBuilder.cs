using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Core.Descriptions;

public interface IListingDescriptionBuilder
{
    string Build(CardListing listing);
}
