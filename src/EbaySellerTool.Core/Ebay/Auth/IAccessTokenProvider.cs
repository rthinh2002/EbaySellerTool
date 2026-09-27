namespace EbaySellerTool.Core.Ebay.Auth;

public interface IAccessTokenProvider
{
    /// <summary>Returns a valid access token, refreshing it first when it is about to expire.</summary>
    /// <exception cref="EbayNotSignedInException">No usable sign-in is stored; the user must sign in again.</exception>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}
