namespace EbaySellerTool.Core.Recognition;

public interface ICardRecognizer
{
    /// <summary>Reads the card's details from an image of a single card. Failures are returned, not thrown.</summary>
    Task<CardRecognitionResult> RecognizeAsync(string imagePath, CancellationToken cancellationToken);
}
