using EbaySellerTool.Core.Cards;

namespace EbaySellerTool.Core.Listing.DryRun;

public interface IDryRunPlanner
{
    DryRunPlan CreatePlan(IReadOnlyList<CardListing> listings);
}
