using ReadingList.Domain;

namespace ReadingList.App;

public interface IBookService
{
    Result<IEnumerable<Book>> GetBooksByAuthor(string author);
}
