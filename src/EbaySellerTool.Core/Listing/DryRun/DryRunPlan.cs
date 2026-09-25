namespace EbaySellerTool.Core.Listing.DryRun;

public sealed record DryRunPlan(IReadOnlyList<DryRunBatch> Batches);
