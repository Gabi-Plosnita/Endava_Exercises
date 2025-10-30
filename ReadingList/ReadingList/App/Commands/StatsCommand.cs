using ReadingList.Domain;

namespace ReadingList.App;

public class StatsCommand(IBookService _bookService) : ICommand
{
    public string Keyword => Resources.StatsCommandKeyword;

    public string Summary => Resources.StatsCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var totalBooks = _bookService.GetAll().Count();
        var finishedCount = _bookService.GetAll().FinishedCount();
        var averageRating = _bookService.GetAll().AverageRating();
        var pagesByGenre = _bookService.GetAll().PagesByGenre();
        var topAuthors = _bookService.GetAll().TopAuthorsByBookCount().ToList();

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
