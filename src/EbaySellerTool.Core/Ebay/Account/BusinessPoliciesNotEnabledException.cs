namespace EbaySellerTool.Core.Ebay.Account;

public sealed class BusinessPoliciesNotEnabledException()
    : Exception("This eBay account hasn't opted in to business policies.");
