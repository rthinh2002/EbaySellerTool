namespace EbaySellerTool.Core.Configuration;

public sealed class CardRecognitionOptions
{
    public const string SectionName = "CardRecognition";

    /// <summary>Anthropic API key, stored in user-secrets as <c>CardRecognition:ApiKey</c>. Falls back to ANTHROPIC_API_KEY.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "claude-opus-5";

    /// <summary>Images are shrunk to this longest side before sending; larger images cost more without reading better.</summary>
    public int MaxImageSide { get; set; } = 1568;

    public bool HasApiKey =>
        !string.IsNullOrWhiteSpace(ApiKey) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
}
