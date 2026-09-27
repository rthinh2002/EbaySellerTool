using System.Net;
using System.Text.Json;

namespace EbaySellerTool.Core.Ebay.Http;

public sealed record EbayResponse<TValue>(HttpStatusCode StatusCode, TValue? Value, IReadOnlyList<EbayError> Errors, Uri? Location)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;

    public bool HasErrorId(int errorId) => Errors.Any(error => error.ErrorId == errorId);

    /// <exception cref="EbayApiException">The request failed as a whole.</exception>
    public EbayResponse<TValue> EnsureSuccess(string operation)
    {
        if (IsSuccess)
        {
            return this;
        }

        var details = Errors.Count == 0 ? StatusCode.ToString() : string.Join("; ", Errors.Select(error => error.ToDisplayString()));
        throw new EbayApiException($"{operation} failed ({(int)StatusCode}): {details}", Errors);
    }

    /// <remarks>
    /// The body is read into <typeparamref name="TValue"/> even on failure, because bulk methods return HTTP 400
    /// with per-item results when every item fails.
    /// </remarks>
    internal static EbayResponse<TValue> Create(HttpStatusCode statusCode, string content, Uri? location)
    {
        var isSuccess = (int)statusCode is >= 200 and < 300;
        var errors = isSuccess ? [] : Deserialize<EbayErrorEnvelope>(content)?.Errors ?? [];

        return new EbayResponse<TValue>(statusCode, Deserialize<TValue>(content), errors, location);
    }

    private static T? Deserialize<T>(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content, EbayJson.Options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
