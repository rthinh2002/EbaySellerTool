namespace EbaySellerTool.Core.Recognition;

/// <summary>Card details read from a card image. Fields are null when they couldn't be read.</summary>
public sealed record RecognizedCard(
    string? Game,
    string? CardName,
    string? SetName,
    string? CardNumber,
    string? Rarity,
    string? Language);
