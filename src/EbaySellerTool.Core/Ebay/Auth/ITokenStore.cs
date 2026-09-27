using EbaySellerTool.Core.Configuration;

namespace EbaySellerTool.Core.Ebay.Auth;

/// <summary>Persists the user's eBay tokens. Implementations must keep them encrypted at rest.</summary>
public interface ITokenStore
{
    Task<EbayToken?> LoadAsync(EbayEnvironment environment, CancellationToken cancellationToken);

    Task SaveAsync(EbayEnvironment environment, EbayToken token, CancellationToken cancellationToken);
}
