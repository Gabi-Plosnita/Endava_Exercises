using ReadingList.Domain;

namespace ReadingList.App;

public class TopRatedCommand(IRepository<Book, int> _repository) : ICommand
{
    public string Keyword => Resources.TopRatedCommandKeyword;

    public string Summary => Resources.TopRatedCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        if(args.Length < 1)
        {
            Console.WriteLine("Error: Missing argument for the number of top rated in top_rated command.");
            return Task.CompletedTask;
        }

        if(!int.TryParse(args[0], out int topRatedNumber) || topRatedNumber <= 0)
        {
            Console.WriteLine("Error: Number of top rated must be a positive integer.");
            return Task.CompletedTask;
        }

        var topRatedBooks = _repository.GetAll().TopRated(topRatedNumber);

        if(!topRatedBooks.Any())
        {
            Console.WriteLine("No books found in the repository.");
            return Task.CompletedTask;
        }

        foreach(var book in topRatedBooks)
        {
            Console.WriteLine(book);
        }

        return Task.CompletedTask;
    }
}
