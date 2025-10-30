using ReadingList.Domain;

namespace ReadingList.App;

public interface IBookService
{
    IEnumerable<Book> GetAll();

    Result<IEnumerable<Book>> GetBooksByAuthor(string author);

    IEnumerable<Book> GetFinished(bool isFinished);

    Result MarkFinished(int id, bool isFinished);
}
