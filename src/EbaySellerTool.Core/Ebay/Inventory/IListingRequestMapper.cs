using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Ebay.Inventory.Models;

namespace EbaySellerTool.Core.Ebay.Inventory;

public interface IListingRequestMapper
{
    InventoryItemRequest MapInventoryItem(CardListing listing, IReadOnlyList<string> imageUrls);

    OfferRequest MapOffer(CardListing listing);
}
