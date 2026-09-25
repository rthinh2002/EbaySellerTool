namespace EbaySellerTool.Core.Configuration;

public sealed class ListingDefaultsOptions
{
    public const string SectionName = "ListingDefaults";

    public string CategoryId { get; set; } = string.Empty;
    public string MerchantLocationKey { get; set; } = string.Empty;
    public string FulfillmentPolicyId { get; set; } = string.Empty;
    public string PaymentPolicyId { get; set; } = string.Empty;
    public string ReturnPolicyId { get; set; } = string.Empty;

    /// <summary>Optional HTML description template. The built-in template is used when blank.</summary>
    public string DescriptionTemplatePath { get; set; } = string.Empty;

    public IReadOnlyList<string> GetMissingRequiredSettings()
    {
        (string Name, string Value)[] requiredSettings =
        [
            (nameof(CategoryId), CategoryId),
            (nameof(MerchantLocationKey), MerchantLocationKey),
            (nameof(FulfillmentPolicyId), FulfillmentPolicyId),
            (nameof(PaymentPolicyId), PaymentPolicyId),
            (nameof(ReturnPolicyId), ReturnPolicyId)
        ];

        return [.. requiredSettings.Where(setting => string.IsNullOrWhiteSpace(setting.Value)).Select(setting => $"{SectionName}:{setting.Name}")];
    }
}
