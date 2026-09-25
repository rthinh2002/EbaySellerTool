using System.Globalization;
using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Validation;

namespace EbaySellerTool.Core.Import;

public sealed class CardListingParser : ICardListingParser
{
    private const int DefaultQuantity = 1;
    private static readonly char[] ImagePathSeparators = [ListingColumns.ImagePathSeparator, '\n', '\r'];

    public RowParseResult Parse(ListingRow row, string imageBaseDirectory)
    {
        var errors = new List<ValidationError>();

        var game = RequireValue(row, ListingColumns.Game, errors);
        var cardName = RequireValue(row, ListingColumns.CardName, errors);
        var condition = ParseCondition(row, errors);
        var price = ParsePrice(row, errors);
        var quantity = ParseQuantity(row, errors);
        EnsureSkuCanBeResolved(row, errors);

        if (errors.Count > 0)
        {
            return RowParseResult.Failure(errors);
        }

        var listing = new CardListing
        {
            RowNumber = row.RowNumber,
            Sku = row.GetValue(ListingColumns.Sku) ?? string.Empty,
            Title = row.GetValue(ListingColumns.Title) ?? string.Empty,
            Game = game!,
            CardName = cardName!,
            SetName = row.GetValue(ListingColumns.SetName),
            CardNumber = row.GetValue(ListingColumns.CardNumber),
            Rarity = row.GetValue(ListingColumns.Rarity),
            Language = row.GetValue(ListingColumns.Language),
            Condition = condition!.Value,
            Quantity = quantity!.Value,
            Price = price!.Value,
            ImagePaths = ParseImagePaths(row.GetValue(ListingColumns.Images), imageBaseDirectory),
            StoreCategory = row.GetValue(ListingColumns.StoreCategory),
            CategoryId = row.GetValue(ListingColumns.CategoryId),
            Description = row.GetValue(ListingColumns.Description),
            AdditionalAspects = row.Aspects
        };

        return RowParseResult.Success(ApplyGeneratedValues(listing));
    }

    private static CardListing ApplyGeneratedValues(CardListing listing) => listing with
    {
        Title = listing.Title.Length > 0 ? listing.Title : ListingTitleBuilder.Build(listing),
        Sku = listing.Sku.Length > 0 ? listing.Sku : SkuGenerator.Generate(listing)!
    };

    private static void EnsureSkuCanBeResolved(ListingRow row, List<ValidationError> errors)
    {
        if (row.GetValue(ListingColumns.Sku) is null && row.GetValue(ListingColumns.CardNumber) is null)
        {
            errors.Add(Error(row.RowNumber, ListingColumns.Sku, "SKU is required when CardNumber is blank."));
        }
    }

    private static string? RequireValue(ListingRow row, ColumnDefinition column, List<ValidationError> errors)
    {
        var value = row.GetValue(column);

        if (value is null)
        {
            errors.Add(Error(row.RowNumber, column, $"{column.Header} is required."));
        }

        return value;
    }

    private static CardCondition? ParseCondition(ListingRow row, List<ValidationError> errors)
    {
        var text = RequireValue(row, ListingColumns.CardCondition, errors);

        if (text is null)
        {
            return null;
        }

        if (CardConditions.TryParse(text, out var condition))
        {
            return condition;
        }

        errors.Add(Error(
            row.RowNumber,
            ListingColumns.CardCondition,
            $"Unknown condition '{text}'. Use one of: {string.Join(", ", CardConditions.AllDisplayNames)}."));
        return null;
    }

    private static decimal? ParsePrice(ListingRow row, List<ValidationError> errors)
    {
        var text = RequireValue(row, ListingColumns.Price, errors);

        if (text is null)
        {
            return null;
        }

        if (decimal.TryParse(text.TrimStart('$'), NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            return price;
        }

        errors.Add(Error(row.RowNumber, ListingColumns.Price, $"Price '{text}' is not a valid number."));
        return null;
    }

    private static int? ParseQuantity(ListingRow row, List<ValidationError> errors)
    {
        var text = row.GetValue(ListingColumns.Quantity);

        if (text is null)
        {
            return DefaultQuantity;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
        {
            return quantity;
        }

        errors.Add(Error(row.RowNumber, ListingColumns.Quantity, $"Quantity '{text}' is not a whole number."));
        return null;
    }

    private static IReadOnlyList<string> ParseImagePaths(string? text, string imageBaseDirectory)
    {
        if (text is null)
        {
            return [];
        }

        // Trimming quotes supports paths pasted from Windows Explorer's "Copy as path".
        return
        [
            .. text.Split(ImagePathSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(path => path.Trim('"'))
                .Where(path => path.Length > 0)
                .Select(path => Path.GetFullPath(path, imageBaseDirectory))
        ];
    }

    private static ValidationError Error(int rowNumber, ColumnDefinition column, string message) =>
        new(rowNumber, column.Header, message);
}
