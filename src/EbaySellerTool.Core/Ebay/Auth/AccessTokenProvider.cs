using EbaySellerTool.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EbaySellerTool.Core.Ebay.Auth;

public sealed class AccessTokenProvider(
    ITokenStore tokenStore,
    IEbayOAuthClient oauthClient,
    IOptions<EbayOptions> ebayOptions,
    TimeProvider timeProvider) : IAccessTokenProvider
{
    // Refresh a little early so a token can't expire in the middle of a batch of requests.
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(5);

    private readonly EbayEnvironment _environment = ebayOptions.Value.Environment;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private EbayToken? _token;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
            _token ??= await tokenStore.LoadAsync(_environment, cancellationToken)
                ?? throw new EbayNotSignedInException($"Not signed in to eBay {_environment}. Run 'ebaytool auth' first.");

            if (IsAboutToExpire(_token.AccessTokenExpiresAt))
            {
                _token = await RefreshAsync(_token, cancellationToken);
            }

            return _token.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<EbayToken> RefreshAsync(EbayToken token, CancellationToken cancellationToken)
    {
        if (IsAboutToExpire(token.RefreshTokenExpiresAt))
        {
            throw new EbayNotSignedInException($"Your eBay {_environment} sign-in has expired. Run 'ebaytool auth' again.");
        }

        var refreshedToken = await oauthClient.RefreshAsync(token, cancellationToken);
        await tokenStore.SaveAsync(_environment, refreshedToken, cancellationToken);

        return refreshedToken;
    }

    private bool IsAboutToExpire(DateTimeOffset expiresAt) => expiresAt - RefreshMargin <= timeProvider.GetUtcNow();
}
