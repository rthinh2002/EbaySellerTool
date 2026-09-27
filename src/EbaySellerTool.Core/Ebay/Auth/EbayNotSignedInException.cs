namespace EbaySellerTool.Core.Ebay.Auth;

public sealed class EbayNotSignedInException(string message) : Exception(message);
