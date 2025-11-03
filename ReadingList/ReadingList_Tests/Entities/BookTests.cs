using AwesomeAssertions;

namespace ReadingList_Tests;

[TestClass]
public class BookTests
{

    [TestMethod]
    public void Validate_ReturnsSuccess_ForValidBook()
    {
        var book = BookHelper.GetValidBook();

        var result = book.Validate();

        result.IsSuccessful.Should().BeTrue();
    }


    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(5.1)]
    public void Validate_ReturnsFailure_WhenRatingIsOutOfBounds(double rating)
    {
        var book = BookHelper.GetValidBook();
        book.Rating = rating;

        var result = book.Validate();

        result.IsFailure.Should().BeTrue();
    }
}
