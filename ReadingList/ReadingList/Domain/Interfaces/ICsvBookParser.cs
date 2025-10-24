namespace ReadingList.Domain;

public interface ICsvBookParser
{
    Result<Book> TryParse(string csvLine);
}
