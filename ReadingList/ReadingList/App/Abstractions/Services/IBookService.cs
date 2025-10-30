using ReadingList.Domain;

namespace ReadingList.App;

public interface IBookService
{
    Result<IEnumerable<Book>> GetBooksByAuthor(string author);

    IEnumerable<Book> GetFinished(bool isFinished);
}
