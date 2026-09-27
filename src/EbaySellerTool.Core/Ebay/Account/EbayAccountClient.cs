using System.Text.Json;
using EbaySellerTool.Core.Configuration;
using EbaySellerTool.Core.Ebay.Http;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Core.Ebay.Account;

public sealed class EbayAccountClient(EbayRestClient restClient, IOptions<EbayOptions> ebayOptions) : IEbayAccountClient
{
    private const int NotEligibleForBusinessPoliciesErrorId = 20403;
    private const string BusinessPolicyProgram = "SELLING_POLICY_MANAGEMENT";

    private readonly string _marketplaceId = ebayOptions.Value.MarketplaceId;

    public async Task<IReadOnlyList<BusinessPolicy>> GetPoliciesAsync(BusinessPolicyType type, CancellationToken cancellationToken)
    {
        var resource = ResourceName(type);
        var url = restClient.ApiUrl($"sell/account/v1/{resource}_policy?marketplace_id={Uri.EscapeDataString(_marketplaceId)}");
        var response = await restClient.GetAsync<JsonElement>(url, cancellationToken);

        if (response.HasErrorId(NotEligibleForBusinessPoliciesErrorId))
        {
            throw new BusinessPoliciesNotEnabledException();
        }

        response.EnsureSuccess($"Reading {resource} policies");
        return ReadPolicies(type, response.Value);
    }

    public async Task OptInToBusinessPoliciesAsync(CancellationToken cancellationToken)
    {
        var response = await restClient.SendJsonAsync<EmptyResponse>(
            HttpMethod.Post,
            restClient.ApiUrl("sell/account/v1/program/opt_in"),
            new { ProgramType = BusinessPolicyProgram },
            cancellationToken);

        response.EnsureSuccess("Opting in to business policies");
    }

    public async Task<BusinessPolicy> CreatePolicyAsync(BusinessPolicyType type, object policy, CancellationToken cancellationToken)
    {
        var resource = ResourceName(type);
        var response = await restClient.SendJsonAsync<JsonElement>(
            HttpMethod.Post, restClient.ApiUrl($"sell/account/v1/{resource}_policy"), policy, cancellationToken);

        return ReadPolicy(type, response.EnsureSuccess($"Creating a {resource} policy").Value);
    }

    // Each policy type uses its own property names, e.g. "fulfillmentPolicies" holding "fulfillmentPolicyId".
    private static List<BusinessPolicy> ReadPolicies(BusinessPolicyType type, JsonElement body) =>
        body.ValueKind == JsonValueKind.Object && body.TryGetProperty($"{ResourceName(type)}Policies", out var policies)
            ? [.. policies.EnumerateArray().Select(policy => ReadPolicy(type, policy))]
            : [];

    private static BusinessPolicy ReadPolicy(BusinessPolicyType type, JsonElement policy) => new(
        type,
        policy.GetProperty($"{ResourceName(type)}PolicyId").GetString()!,
        policy.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty);

    private static string ResourceName(BusinessPolicyType type) => type.ToString().ToLowerInvariant();
}
