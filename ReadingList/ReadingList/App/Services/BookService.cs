using ReadingList.Domain;

namespace ReadingList.App;

public class BookService(IRepository<Book, int> _repository) : IBookService
{
    public IEnumerable<Book> GetAll()
    {
        return _repository.GetAll();
    }

    public Book? GetById(int id)
    {
        return _repository.GetById(id);
    }

    public Result<IEnumerable<Book>> GetBooksByAuthor(string author)
    {
        var result = new Result<IEnumerable<Book>>();
        result.Value = _repository.GetAll().ByAuthor(author);
        if (!result.Value.Any())
        {
            result.AddError($"No books found by author: {author}.");
        }
        return result;
    }

    public IEnumerable<Book> GetFinished(bool isFinished)
    {
        return _repository.GetAll().Finished(isFinished);
    }

    public Result Update(int id, Book book)
    {
        var result = new Result();
        var bookToUpdate = _repository.GetById(id);
        if (bookToUpdate == null)
        {
            result.AddError($"No book found with ID {id}.");
            return result;
        }

        bookToUpdate.CopyFrom(book);
        var updateSuccess = _repository.Update(bookToUpdate);
        if (!updateSuccess)
        {
            result.AddError($"Failed to update book ID {id}.");
        }

        return result;
    }
}
