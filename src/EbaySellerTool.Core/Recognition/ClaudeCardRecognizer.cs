using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using EbaySellerTool.Core.Configuration;
using Microsoft.Extensions.Options;
using OpenCvSharp;

namespace EbaySellerTool.Core.Recognition;

/// <summary>Reads card details from a card photo with Claude's vision, returning them as structured JSON.</summary>
public sealed class ClaudeCardRecognizer(IOptions<CardRecognitionOptions> recognitionOptions) : ICardRecognizer
{
    private const int MaxOutputTokens = 1024;
    private const int JpegQuality = 90;

    private const string Instructions = """
        This is a scan of a single trading card (for example Yu-Gi-Oh!, Riftbound, Pokémon or Magic: The Gathering).
        Read the details printed on the card for an eBay listing:
        - game: the trading card game's usual name, e.g. "Yu-Gi-Oh! TCG", "Riftbound", "Pokémon TCG".
        - cardName: the card's name exactly as printed.
        - setName: the set or expansion name if you can identify it from the set code or printing, otherwise null.
        - cardNumber: the collector number or set code as printed, e.g. "LOB-EN001" (Yu-Gi-Oh!) or "OGN-006" for "OGN • 006/298" (Riftbound).
        - rarity: the rarity if it is printed or indicated by the rarity symbol or foil treatment, otherwise null.
        - language: the language of the card text, e.g. "English".
        Use null for anything you cannot read or identify with confidence rather than guessing.
        """;

    private static readonly string[] Fields = ["game", "cardName", "setName", "cardNumber", "rarity", "language"];

    private readonly CardRecognitionOptions _options = recognitionOptions.Value;
    private readonly Lazy<AnthropicClient> _client = new(() => CreateClient(recognitionOptions.Value));

    public async Task<CardRecognitionResult> RecognizeAsync(string imagePath, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.Value.Beta.Messages.Create(CreateRequest(EncodeImage(imagePath)), cancellationToken);
            return ReadResult(response);
        }
        catch (AnthropicApiException exception)
        {
            return CardRecognitionResult.Failure($"Claude request failed: {exception.Message}");
        }
    }

    private MessageCreateParams CreateRequest(string base64Jpeg) => new()
    {
        Model = _options.Model,
        MaxTokens = MaxOutputTokens,
        // Server-side fallback: if the model declines, eBay-listing work shouldn't just stop.
        Betas = [AnthropicBeta.ServerSideFallback2026_07_01],
        Fallbacks = new Default(),
        OutputConfig = new BetaOutputConfig { Format = new BetaJsonOutputFormat { Schema = CreateSchema() } },
        Messages =
        [
            new BetaMessageParam
            {
                Role = Role.User,
                Content = new List<BetaContentBlockParam>
                {
                    new BetaImageBlockParam { Source = new BetaBase64ImageSource { Data = base64Jpeg, MediaType = MediaType.ImageJpeg } },
                    new BetaTextBlockParam { Text = Instructions },
                },
            },
        ],
    };

    private static CardRecognitionResult ReadResult(BetaMessage response)
    {
        if (response.StopReason == "refusal")
        {
            return CardRecognitionResult.Failure("Claude declined to read this image.");
        }

        var json = string.Concat(response.Content.Select(block => block.Value).OfType<BetaTextBlock>().Select(text => text.Text));

        try
        {
            var card = JsonSerializer.Deserialize<RecognizedCard>(json, JsonOptions);
            return card is null ? CardRecognitionResult.Failure("Claude returned no card details.") : CardRecognitionResult.Success(Clean(card));
        }
        catch (JsonException)
        {
            return CardRecognitionResult.Failure("Claude's answer wasn't valid card details.");
        }
    }

    private static RecognizedCard Clean(RecognizedCard card) => new(
        NullIfBlank(card.Game),
        NullIfBlank(card.CardName),
        NullIfBlank(card.SetName),
        NullIfBlank(card.CardNumber),
        NullIfBlank(card.Rarity),
        NullIfBlank(card.Language));

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string EncodeImage(string imagePath)
    {
        using var image = Cv2.ImRead(imagePath, ImreadModes.Color);

        if (image.Empty())
        {
            throw new InvalidDataException($"Could not read image {imagePath}.");
        }

        using var resized = ShrinkToMaxSide(image, _options.MaxImageSide);
        Cv2.ImEncode(".jpg", resized, out var jpeg, new ImageEncodingParam(ImwriteFlags.JpegQuality, JpegQuality));

        return Convert.ToBase64String(jpeg);
    }

    private static Mat ShrinkToMaxSide(Mat image, int maxSide)
    {
        var scale = (double)maxSide / Math.Max(image.Width, image.Height);

        if (scale >= 1)
        {
            return image.Clone();
        }

        var resized = new Mat();
        Cv2.Resize(image, resized, new Size(), scale, scale, InterpolationFlags.Area);
        return resized;
    }

    private static Dictionary<string, JsonElement> CreateSchema()
    {
        var nullableString = new { type = new[] { "string", "null" } };

        return new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(Fields.ToDictionary(field => field, _ => (object)nullableString)),
            ["required"] = JsonSerializer.SerializeToElement(Fields),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        };
    }

    private static AnthropicClient CreateClient(CardRecognitionOptions options) =>
        string.IsNullOrWhiteSpace(options.ApiKey) ? new AnthropicClient() : new AnthropicClient { ApiKey = options.ApiKey };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
