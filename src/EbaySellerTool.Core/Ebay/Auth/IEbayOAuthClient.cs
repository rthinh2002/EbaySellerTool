namespace EbaySellerTool.Core.Ebay.Auth;

public interface IEbayOAuthClient
{
    /// <summary>The eBay sign-in page where the user grants the tool access to their account.</summary>
    Uri BuildConsentUrl(string state);

    Task<EbayToken> ExchangeAuthorizationCodeAsync(string authorizationCode, CancellationToken cancellationToken);

    /// <summary>Gets a new access token. The refresh token itself stays the same.</summary>
    Task<EbayToken> RefreshAsync(EbayToken token, CancellationToken cancellationToken);
}
