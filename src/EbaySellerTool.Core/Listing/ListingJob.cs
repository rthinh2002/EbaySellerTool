using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Ebay;

namespace EbaySellerTool.Core.Listing;

/// <summary>Tracks one card's progress through the listing steps.</summary>
public sealed class ListingJob(CardListing listing)
{
    private const string IncompleteMessage = "Listing did not complete.";

    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];
    private ListingStatus? _finalStatus;

    public CardListing Listing { get; } = listing;
    public string Sku => Listing.Sku;
    public IReadOnlyList<string> ImageUrls { get; private set; } = [];
    public string? OfferId { get; private set; }
    public string? ListingId { get; private set; }

    public bool IsActive => _finalStatus is null;

    public void SetImageUrls(IReadOnlyList<string> imageUrls) => ImageUrls = imageUrls;

    public void SetOfferId(string offerId) => OfferId = offerId;

    public void MarkListed(string listingId) => Complete(ListingStatus.Listed, listingId);

    public void MarkRevised(string listingId) => Complete(ListingStatus.Revised, listingId);

    public void Fail(string error) => Fail([error]);

    public void Fail(IEnumerable<EbayError> errors) => Fail(errors.Select(error => error.ToDisplayString()));

    public void Fail(IEnumerable<string> errors)
    {
        _errors.AddRange(errors);
        _finalStatus = ListingStatus.Failed;
    }

    public void AddWarnings(IEnumerable<EbayError> warnings) =>
        _warnings.AddRange(warnings.Select(warning => warning.ToDisplayString()));

    public ListingOutcome ToOutcome() => new()
    {
        RowNumber = Listing.RowNumber,
        Sku = Sku,
        Title = Listing.Title,
        Status = _finalStatus ?? ListingStatus.Failed,
        OfferId = OfferId,
        ListingId = ListingId,
        Errors = _finalStatus is null ? [.. _errors, IncompleteMessage] : [.. _errors],
        Warnings = [.. _warnings]
    };

    private void Complete(ListingStatus status, string listingId)
    {
        ListingId = listingId;
        _finalStatus = status;
    }
}
