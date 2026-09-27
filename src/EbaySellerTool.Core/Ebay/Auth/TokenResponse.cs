using System.Text.Json.Serialization;

namespace EbaySellerTool.Core.Ebay.Auth;

internal sealed record TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresInSeconds { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("refresh_token_expires_in")]
    public int? RefreshTokenExpiresInSeconds { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}
