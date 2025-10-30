using ReadingList.Domain;

namespace ReadingList.App;

public interface IBookService
{
    IEnumerable<Book> GetAll();

    Book? GetById(int id);

    Result<IEnumerable<Book>> GetBooksByAuthor(string author);

    IEnumerable<Book> GetFinished(bool isFinished);

    Result Update(int id, Book book);
}
