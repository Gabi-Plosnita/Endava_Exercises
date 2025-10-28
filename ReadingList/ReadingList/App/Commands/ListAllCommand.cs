
using ReadingList.Domain;

namespace ReadingList.App;

public class ListAllCommand(IRepository<Book, int> _repository) : ICommand
{
    public string Keyword => "list_all";

    public string Summary => "Lists all books";

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var books = _repository.All();
        if (!books.Any())
        {
            Console.WriteLine("No books found.");
            return Task.CompletedTask;
        }
        foreach (var book in books)
        {
            Console.WriteLine(book);
        }
        return Task.CompletedTask;
    }
}
