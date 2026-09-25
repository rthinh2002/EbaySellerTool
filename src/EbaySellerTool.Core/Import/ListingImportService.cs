using EbaySellerTool.Core.Cards;
using EbaySellerTool.Core.Excel;
using EbaySellerTool.Core.Validation;

namespace EbaySellerTool.Core.Import;

public sealed class ListingImportService(
    IListingSheetReader sheetReader,
    ICardListingParser parser,
    ICardListingValidator validator) : IListingImportService
{
    public ListingImportResult Import(string filePath)
    {
        var sheet = sheetReader.Read(filePath);

        var missingColumnErrors = FindMissingRequiredColumns(sheet);
        if (missingColumnErrors.Count > 0)
        {
            return ListingImportResult.SheetFailure(missingColumnErrors);
        }

        var imageBaseDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        var parseResults = sheet.Rows.Select(row => parser.Parse(row, imageBaseDirectory)).ToList();
        var parsedListings = parseResults.Select(result => result.Listing).OfType<CardListing>().ToList();

        var errors = parseResults
            .SelectMany(result => result.Errors)
            .Concat(validator.Validate(parsedListings))
            .OrderBy(error => error.RowNumber)
            .ToList();

        var invalidRowNumbers = errors.Select(error => error.RowNumber).ToHashSet();
        var validListings = parsedListings.Where(listing => !invalidRowNumbers.Contains(listing.RowNumber)).ToList();

        return new ListingImportResult(sheet.Rows.Count, validListings, errors);
    }

    private static List<ValidationError> FindMissingRequiredColumns(ListingSheet sheet) =>
        ListingColumns.Required
            .Where(column => !sheet.HasColumn(column))
            .Select(column => new ValidationError(null, column.Header, $"Required column '{column.Header}' is missing from the sheet."))
            .ToList();
}
