namespace EbaySellerTool.Core.Ebay.Account;

/// <summary>eBay Sell Account API: the seller's business policies for the configured marketplace.</summary>
public interface IEbayAccountClient
{
    /// <exception cref="BusinessPoliciesNotEnabledException">The seller hasn't opted in to business policies.</exception>
    Task<IReadOnlyList<BusinessPolicy>> GetPoliciesAsync(BusinessPolicyType type, CancellationToken cancellationToken);

    Task OptInToBusinessPoliciesAsync(CancellationToken cancellationToken);

    /// <param name="policy">The policy body in eBay's format, e.g. from <see cref="SandboxTestPolicies"/>.</param>
    Task<BusinessPolicy> CreatePolicyAsync(BusinessPolicyType type, object policy, CancellationToken cancellationToken);
}
