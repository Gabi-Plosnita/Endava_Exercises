using ReadingList.Domain;

namespace ReadingList.App;

public class StatsCommand(IRepository<Book, int> _repository) : ICommand
{
    public string Keyword => Resources.StatsCommandKeyword;

    public string Summary => Resources.StatsCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var totalBooks = _repository.All().Count();
        var finishedCount = _repository.All().FinishedCount();
        var averageRating = _repository.All().AverageRating();
        var pagesByGenre = _repository.All().PagesByGenre();
        var topAuthors = _repository.All().TopAuthorsByBookCount().ToList();

        if(totalBooks == 0)
        {
            Console.WriteLine("No books imported");
            return Task.CompletedTask;
        }

        Console.WriteLine("=== Reading List Statistics ===");
        Console.WriteLine($"Total Books: {totalBooks}");
        Console.WriteLine($"Finished Books: {finishedCount}");
        Console.WriteLine($"Average Rating: {averageRating:F2}");

        Console.WriteLine("Pages by Genre:");
        foreach (var genreEntry in pagesByGenre)
        {
            Console.WriteLine($"- {genreEntry.Key}: {genreEntry.Value} pages");
        }
        Console.WriteLine("Top 3 Authors by Book Count:");
        foreach (var author in topAuthors)
        {
            Console.WriteLine($"- {author}");
        }
        return Task.CompletedTask;
    }
}
