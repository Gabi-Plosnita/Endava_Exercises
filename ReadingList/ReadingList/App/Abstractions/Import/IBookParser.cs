using ReadingList.Domain;

namespace ReadingList.App;

public interface IBookParser
{
    Result<Book> TryParse(string csvLine);
}
