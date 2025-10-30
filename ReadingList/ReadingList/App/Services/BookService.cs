using ReadingList.Domain;

namespace ReadingList.App;

public class BookService(IRepository<Book, int> _repository) : IBookService
{
    public Result<IEnumerable<Book>> GetBooksByAuthor(string author)
    {
        var result = new Result<IEnumerable<Book>>();
        result.Value = _repository.All().ByAuthor(author);
        if (!result.Value.Any())
        {
            result.AddError($"No books found by author: {author}.");
        }
        return result;
    }

    public IEnumerable<Book> GetFinished(bool isFinished)
    {
        return _repository.All().Finished(isFinished);
    }
}
