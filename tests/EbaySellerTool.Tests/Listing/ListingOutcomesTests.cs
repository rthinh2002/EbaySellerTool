using EbaySellerTool.Core.Listing;
using EbaySellerTool.Core.Validation;

namespace EbaySellerTool.Tests.Listing;

public class ListingOutcomesTests
{
    [Fact]
    public void ForInvalidRows_GroupsErrorsByRowAndSkipsSheetErrors()
    {
        ValidationError[] errors =
        [
            new(4, "Price", "Price must be greater than 0."),
            new(4, "Images", "Image not found: a.jpg"),
            new(7, null, "Something else"),
            new(null, "Game", "Required column 'Game' is missing.")
        ];

        var outcomes = ListingOutcomes.ForInvalidRows(errors).ToList();

        Assert.Equal([4, 7], outcomes.Select(outcome => outcome.RowNumber));
        Assert.All(outcomes, outcome => Assert.Equal(ListingStatus.Invalid, outcome.Status));
        Assert.Equal(["Price: Price must be greater than 0.", "Images: Image not found: a.jpg"], outcomes[0].Errors);
        Assert.Equal(["Something else"], outcomes[1].Errors);
    }

    [Fact]
    public void ListingRunResult_OrdersOutcomesByRow()
    {
        var result = new ListingRunResult(
        [
            new ListingOutcome { RowNumber = 5, Status = ListingStatus.Listed },
            new ListingOutcome { RowNumber = 2, Status = ListingStatus.Invalid }
        ]);

        Assert.Equal([2, 5], result.Outcomes.Select(outcome => outcome.RowNumber));
        Assert.True(result.HasFailures);
        Assert.Equal(1, result.Count(ListingStatus.Listed));
    }
}
