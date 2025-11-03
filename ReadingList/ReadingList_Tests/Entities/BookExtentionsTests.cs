using AwesomeAssertions;
using ReadingList.Domain;
using System.Data;

namespace ReadingList_Tests;

[TestClass]
public class BookExtensionsTests
{
    private static List<Book> SampleBooks() => new()
        {
            new Book { Id = 1, Title = "Alpha",   Author = "Alice",   Year = 2020, Pages = 100, Genre = (Genre)1, Finished = true,  Rating = 4.5 },
            new Book { Id = 2, Title = "Beta",    Author = "Bob",     Year = 2021, Pages = 200, Genre = (Genre)1, Finished = false, Rating = 4.0 },
            new Book { Id = 3, Title = "Gamma",   Author = "Alice",   Year = 2019, Pages = 150, Genre = (Genre)2, Finished = true,  Rating = 5.0 },
            new Book { Id = 4, Title = "Delta",   Author = "Charlie", Year = 2018, Pages = 120, Genre = (Genre)2, Finished = true,  Rating = 3.5 },
            new Book { Id = 5, Title = "Epsilon", Author = "Bob",     Year = 2022, Pages = 300, Genre = (Genre)1, Finished = false, Rating = 2.5 },
            new Book { Id = 6, Title = "Zeta",    Author = "Alice",   Year = 2023, Pages = 220, Genre = (Genre)3, Finished = false, Rating = 4.5 },
        };

    [TestMethod]
    public void FinishedCount_ReturnsNumberOfFinishedBooks()
    {
        var books = SampleBooks();

        var count = books.FinishedCount();

        count.Should().Be(3); 
    }

    [TestMethod]
    public void FinishedCount_ReturnsZero_WhenCollectionIsEmpty()
    {
        var books = new List<Book>();

        var count = books.FinishedCount();

        count.Should().Be(0);
    }

    [TestMethod]
    public void FinishedCount_Throws_WhenCollectionIsNull()
    {
        IEnumerable<Book> books = null!;

        var act = () => books.FinishedCount();

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AverageRating_ComputesAverage()
    {
        var books = SampleBooks();

        var avg = books.AverageRating();

        avg.Should().BeApproximately(4, 0.01);
    }

    [TestMethod]
    public void AverageRating_ReturnsZero_WhenCollectionIsEmpty()
    {
        var books = new List<Book>();

        var avg = books.AverageRating();

        avg.Should().Be(0.0);
    }

    [TestMethod]
    public void AverageRating_Throws_WhenCollectionIsNull()
    {
        IEnumerable<Book> books = null!;

        var act = () => books.AverageRating();

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void PagesByGenre_SumsPagesPerGenre()
    {
        var books = SampleBooks();

        var dict = books.PagesByGenre();

        dict.Should().HaveCount(3);
        dict[(Genre)1].Should().Be(100 + 200 + 300); 
        dict[(Genre)2].Should().Be(150 + 120);      
        dict[(Genre)3].Should().Be(220);             
    }

    [TestMethod]
    public void PagesByGenre_ReturnsEmptyDictionary_WhenCollectionIsEmpty()
    {
        var books = new List<Book>();

        var dict = books.PagesByGenre();

        dict.Should().BeEmpty();
    }

    [TestMethod]
    public void PagesByGenre_Throws_WhenCollectionIsNull()
    {
        IEnumerable<Book> books = null!;

        var act = () => books.PagesByGenre();

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void TopAuthorsByBookCount_ReturnsTopN()
    {
        var books = SampleBooks();

        var topAuthors = books.TopAuthorsByBookCount(3).ToList();

        topAuthors.Should().Equal("Alice", "Bob", "Charlie");
    }

    [TestMethod]
    public void TopAuthorsByBookCount_ReturnsTopNAndTieBreaksByName()
    {
        var books = new List<Book>
            {
                new Book { Id=1, Title="A1", Author="Alice", Year=2020, Pages=100, Genre=(Genre)1, Finished=true, Rating=4 },
                new Book { Id=2, Title="A2", Author="Alice", Year=2020, Pages=100, Genre=(Genre)1, Finished=true, Rating=4 },

                new Book { Id=3, Title="B1", Author="Bob",   Year=2020, Pages=100, Genre=(Genre)1, Finished=true, Rating=4 },
                new Book { Id=4, Title="B2", Author="Bob",   Year=2020, Pages=100, Genre=(Genre)1, Finished=true, Rating=4 },

                new Book { Id=5, Title="Ad1", Author="Adam", Year=2020, Pages=100, Genre=(Genre)1, Finished=true, Rating=4 },
                new Book { Id=6, Title="Ad2", Author="Adam", Year=2020, Pages=100, Genre=(Genre)1, Finished=true, Rating=4 },

                new Book { Id=7, Title="Z1", Author="Zoe",   Year=2020, Pages=100, Genre=(Genre)1, Finished=true, Rating=4 },
            };

        var topAuthors = books.TopAuthorsByBookCount(2).ToList();

        topAuthors.Should().Equal("Adam", "Alice"); 
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void TopAuthorsByBookCount_ReturnsZero_ForNotPositiveValues(int topN)
    {
        var books = SampleBooks();

        var topAuthors = books.TopAuthorsByBookCount(topN);

        topAuthors.Should().BeEmpty();
    }

    [TestMethod]
    public void TopAuthorsByBookCount_ReturnsEmpty_WhenCollectionIsEmpty()
    {
        var books = new List<Book>();

        var topAuthors = books.TopAuthorsByBookCount(3);

        topAuthors.Should().BeEmpty();
    }

    [TestMethod]
    public void TopAuthorsByBookCount_Throws_WhenCollectionIsNull()
    {
        IEnumerable<Book> books = null!;

        var act = () => books.TopAuthorsByBookCount(3);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void ByAuthor_FiltersCaseInsensitive()
    {
        var books = SampleBooks();
        var author = "alice";

        var result = books.ByAuthor(author).ToList();

        result.Should().HaveCount(3);
        result.Select(b => b.Author).Should().OnlyContain(a => a == "Alice");
    }

    [TestMethod]
    public void ByAuthor_ReturnsEmpty_WhenAuthorNotFound()
    {
        var books = SampleBooks();
        var author = "Nobody";

        var result = books.ByAuthor(author).ToList();

        result.Should().BeEmpty();
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow(null)]

    public void ByAuthor_ReturnsEmpty_WhenAuthorIsNullOrWhiteSpace(string author)
    {
        var books = SampleBooks();

        var result = books.ByAuthor(author);

        result.Should().BeEmpty(); 
    }

    [TestMethod]
    public void ByAuthor_Throws_WhenCollectionIsNull()
    {
        IEnumerable<Book> books = null!;
        var author = "Alice";

        var act = () => books.ByAuthor(author);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Finished_ReturnsOnlyMatching()
    {
        var books = SampleBooks();

        var finished = books.Finished(isFinished: true).ToList();
        var notFinished = books.Finished(isFinished: false).ToList();

        finished.Should().OnlyContain(b => b.Finished);
        notFinished.Should().OnlyContain(b => !b.Finished);
        finished.Count.Should().Be(3);
        notFinished.Count.Should().Be(3);
    }

    [TestMethod]
    public void Finished_Throws_WhenCollectionIsNull()
    {
        IEnumerable<Book> books = null!;

        var act = () => books.Finished(isFinished: true);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void TopRated_ReturnsBooksOrderedByRatingDescending_WhenCollectionHasBooks()
    {
        var books = new List<Book>
            {
                new Book { Id=1, Title="B", Author="A", Year=2020, Pages=100, Genre=(Genre)1, Finished=true,  Rating=4.5 },
                new Book { Id=2, Title="A", Author="A", Year=2020, Pages=100, Genre=(Genre)1, Finished=true,  Rating=4.5 }, 
                new Book { Id=3, Title="C", Author="A", Year=2020, Pages=100, Genre=(Genre)1, Finished=false, Rating=5.0 },
                new Book { Id=4, Title="D", Author="A", Year=2020, Pages=100, Genre=(Genre)1, Finished=false, Rating=3.0 },
            };

        var top2 = books.TopRated(2).ToList();

        top2.Should().HaveCount(2);
        top2[0].Title.Should().Be("C"); 
        top2[1].Title.Should().Be("A"); 
    }

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void TopRated_ReturnsEmpty_WhenTopNIsZeroOrNegative(int topN)
    {
        var books = SampleBooks();

        var result = books.TopRated(topN);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void TopRated_ReturnsEmpty_WhenCollectionIsEmpty()
    {
        var books = new List<Book>();

        var result = books.TopRated(3);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void TopRated_Throws_WhenCollectionIsNull()
    {
        IEnumerable<Book> books = null!;

        var act = () => books.TopRated(3);

        act.Should().Throw<ArgumentNullException>();
    }
}
