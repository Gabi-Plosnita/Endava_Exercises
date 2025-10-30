using ReadingList.Domain;
using System.Net;

namespace ReadingList.App;

public class BookService(IRepository<Book, int> _repository) : IBookService
{
    public IEnumerable<Book> GetAll()
    {
        return _repository.GetAll();
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

    public Result MarkFinished(int id, bool isFinished)
    {
        var result = new Result();
        if (!_repository.TryGet(id, out var foundBook) || foundBook == null)
        {
            result.AddError($"No book found with ID {id}.");
            return result;
        }

        foundBook.Finished = true;
        var updateSuccess = _repository.Update(foundBook);
        if (!updateSuccess)
        {
            result.AddError($"Failed to update book ID {id}.");
        }

        return result;
    }
}
