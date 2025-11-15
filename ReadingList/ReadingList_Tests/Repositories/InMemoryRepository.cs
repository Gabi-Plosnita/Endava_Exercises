using AwesomeAssertions;
using ReadingList.Domain;
using ReadingList.Infrastructure;

namespace ReadingList_Tests;

[TestClass]
public class InMemoryRepositoryTests
{
    private InMemoryRepository<Book, int> _repository = default!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new InMemoryRepository<Book, int>(book => book.Id);
    }

    [TestMethod]
    public void Add_ReturnsTrue_WhenItemIsAddedSuccessfully()
    {
        var book = BookHelper.GetValidBook();

        var result = _repository.Add(book);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void Add_ReturnsFalse_WhenItemWithSameKeyExists()
    {
        var book = BookHelper.GetValidBook();
        _repository.Add(book);
        var duplicateBook = BookHelper.GetValidBook();

        var result = _repository.Add(duplicateBook);

        result.Should().BeFalse();
    }

    [TestMethod]
    public void Update_ReturnsTrue_WhenItemIsUpdatedSuccessfully()
    {
        var book = BookHelper.GetValidBook();
        _repository.Add(book);
        book.Title = "Updated Title";

        var result = _repository.Update(book);

        var updatedBook = _repository.GetById(book.Id);
        result.Should().BeTrue();
        updatedBook.Should().NotBeNull();
        updatedBook.Title.Should().Be("Updated Title");
    }

    [TestMethod]
    public void Update_ReturnsFalse_WhenItemDoesNotExist()
    {
        var book = BookHelper.GetValidBook();

        var result = _repository.Update(book);

        result.Should().BeFalse();
    }
}
