using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EbaySellerTool.Core.Ebay;

public static class EbayJson
{
    public static JsonSerializerOptions Options { get; } = Create(writeIndented: false);

    /// <summary>
    /// Human-readable output for files people review, such as dry-run plans. Relaxed escaping keeps HTML
    /// descriptions readable; it is only safe because the output is never embedded in a web page.
    /// </summary>
    public static JsonSerializerOptions IndentedOptions { get; } = Create(writeIndented: true, JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

    private static JsonSerializerOptions Create(bool writeIndented, JavaScriptEncoder? encoder = null) => new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = writeIndented,
        Encoder = encoder
    };
}
