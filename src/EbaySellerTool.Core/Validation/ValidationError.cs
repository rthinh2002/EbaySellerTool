namespace EbaySellerTool.Core.Validation;

/// <param name="RowNumber">Excel row number, or null for sheet-level errors.</param>
public sealed record ValidationError(int? RowNumber, string? Column, string Message);
