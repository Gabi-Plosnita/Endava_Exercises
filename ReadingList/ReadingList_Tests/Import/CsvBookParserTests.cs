using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ReadingList.Domain;
using ReadingList.Infrastructure;
using System.Globalization;

namespace ReadingList_Tests;

[TestClass]
public class CsvBookParserTests
{
    private readonly CsvBookParser _parser = new();

    private static string MakeCsvLine(
        object id, object title, object author, object year, object pages,
        object genre, object finished, object rating)
    {
        return string.Join(",", id, title, author, year, pages, genre, finished, rating);
    }

    [TestMethod]
    public void TryParse_ReturnsSuccessAndMapsAllFields_WhenLineIsValid()
    {
        var line = MakeCsvLine(
            1, "Clean Code", "Robert C. Martin", 2008, 464, "Software", true, 4.5);

        var result = _parser.TryParse(line);

        using (new AssertionScope())
        {
            result.IsSuccessful.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(1);
            result.Value.Title.Should().Be("Clean Code");
            result.Value.Author.Should().Be("Robert C. Martin");
            result.Value.Year.Should().Be(2008);
            result.Value.Pages.Should().Be(464);
            result.Value.Genre.Should().Be(Genre.Software);
            result.Value.Finished.Should().BeTrue();
            result.Value.Rating.Should().Be(4.5);
        }
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void TryParse_ReturnsFailure_WhenInputIsNullOrEmptyOrWhitespace(string line)
    {
        var result = _parser.TryParse(line);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Contain("null or empty");
    }

    [TestMethod]
    public void TryParse_ReturnsFailure_WhenFieldConversionIsInvalid()
    {
        var line = MakeCsvLine(
            "not-an-int", "T", "A", 2020, 100, 1, true, 3.0);

        var result = _parser.TryParse(line);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Contains("CSV parse error"));
    }

    [TestMethod]
    public void TryParse_ReturnsFailure_WhenMissingFields()
    {
        var line = "1,Title,Author,2020,300,1,true";

        var result = _parser.TryParse(line);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("CSV parse error"));
    }

    [TestMethod]
    public void TryParse_ReturnsFailure_WhenDomainValidationFails()
    {
        var line = MakeCsvLine(
            1, "Any", "Someone", 2020, 123, 1, false, 6.0);

        var result = _parser.TryParse(line);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Contains("Validation failed") && e.Contains("Rating must be between 0 and 5"));
    }

    [TestMethod]
    public void TryParse_ReturnsFailure_WhenNoRecordsRead()
    {
        var line = ","; 

        var result = _parser.TryParse(line);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("CSV parse error"));
    }
}