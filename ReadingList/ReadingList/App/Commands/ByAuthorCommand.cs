
using ReadingList.Domain;

namespace ReadingList.App;

public class ByAuthorCommand(IRepository<Book, int> _repository) : ICommand
{
    public string Keyword => Resources.ByAuthorCommandKeyword;

    public string Summary => Resources.ByAuthorCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var author = string.Join(" ", args);

        if(string.IsNullOrWhiteSpace(author))
        {
            Console.WriteLine("Error: author_name cannot be empty.");
            return Task.CompletedTask;
        }

        var byAuthorBooks = _repository.All()
                                       .Where(b => b.Author.Equals(author, StringComparison.OrdinalIgnoreCase));

        if(!byAuthorBooks.Any())
        {
            Console.WriteLine($"No books found by author: {author}.");
            return Task.CompletedTask;
        }

        foreach(var book in byAuthorBooks)
        {
            Console.WriteLine(book);
        }

        return Task.CompletedTask;
    }
}
