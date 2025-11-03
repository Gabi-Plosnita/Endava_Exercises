using AwesomeAssertions;
using ReadingList.Domain;

namespace ReadingList_Tests;

[TestClass]
public class BookTests
{
    private Book GetValidBook() => new Book()
    {
        Id = 2,
        Title = "Valid Book",
        Author = "Valid Author",
        Year = 2021,
        Pages = 250,
        Genre = Genre.History,
        Finished = false,
        Rating = 4.5
    };


    [TestMethod]
    public void Validate_ReturnsSuccess_ForValidBook()
    {
        var book = GetValidBook();

        var result = book.Validate();

        result.IsSuccessful.Should().BeTrue();
    }


    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(5.1)]
    public void Validate_ReturnsFailure_WhenRatingIsOutOfBounds(double rating)
    {
        var book = GetValidBook();
        book.Rating = rating;

        var result = book.Validate();

        result.IsFailure.Should().BeTrue();
    }
}
