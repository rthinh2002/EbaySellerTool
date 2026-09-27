namespace EbaySellerTool.Core.Ebay.Auth;

public sealed record EbayToken(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
