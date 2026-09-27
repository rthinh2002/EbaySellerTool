using EbaySellerTool.Core.Configuration;

namespace EbaySellerTool.Core.Ebay.Account;

/// <summary>
/// Minimal business policies for Sandbox test users, which start with none and have no Seller Hub to create them in.
/// Real (Production) accounts use the seller's own policies.
/// </summary>
public static class SandboxTestPolicies
{
    private const string CategoryType = "ALL_EXCLUDING_MOTORS_VEHICLES";

    public static object Create(BusinessPolicyType type, EbayOptions ebay)
    {
        var name = $"EbaySellerTool test {type.ToString().ToLowerInvariant()}";
        object[] categoryTypes = [new { Name = CategoryType }];

        return type switch
        {
            BusinessPolicyType.Payment => new { name, ebay.MarketplaceId, categoryTypes, ImmediatePay = true },
            BusinessPolicyType.Return => new
            {
                name,
                ebay.MarketplaceId,
                categoryTypes,
                ReturnsAccepted = true,
                ReturnPeriod = new { Value = 30, Unit = "DAY" },
                ReturnShippingCostPayer = "BUYER",
                RefundMethod = "MONEY_BACK"
            },
            BusinessPolicyType.Fulfillment => new
            {
                name,
                ebay.MarketplaceId,
                categoryTypes,
                HandlingTime = new { Value = 2, Unit = "DAY" },
                ShippingOptions = new[]
                {
                    new
                    {
                        OptionType = "DOMESTIC",
                        CostType = "FLAT_RATE",
                        ShippingServices = new[]
                        {
                            new { ShippingServiceCode = "AU_Regular", ShippingCost = new { Value = "2.00", ebay.Currency }, SortOrder = 1 }
                        }
                    }
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
