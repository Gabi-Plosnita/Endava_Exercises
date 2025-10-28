using ReadingList.Domain;

namespace ReadingList.App;

public class RateCommand(IRepository<Book, int> _repository) : ICommand
{
    public string Keyword => Resources.RateCommandKeyword;

    public string Summary => Resources.RateCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Missing arguments. Usage: rate <bookId> <rating (0–5)>");
            return Task.CompletedTask;
        }

        if (!int.TryParse(args[0], out var bookId))
        {
            Console.WriteLine("Error: Invalid book ID. It must be a number.");
            return Task.CompletedTask;
        }

        if (!double.TryParse(args[1], out var rating))
        {
            Console.WriteLine("Error: Invalid rating. It must be a number between 0 and 5.");
            return Task.CompletedTask;
        }

        if (rating < 0 || rating > 5)
        {
            Console.WriteLine("Error: Rating must be between 0 and 5.");
            return Task.CompletedTask;
        }

        if (!_repository.TryGet(bookId, out var book) || book == null)
        {
            Console.WriteLine($"Error: Book with ID {bookId} not found.");
            return Task.CompletedTask;
        }

        book.Rating = rating;

        if (_repository.Update(book))
        {
            Console.WriteLine($"Successfully rated '{book.Title}' by {book.Author} — {rating}");
        }
        else
        {
            Console.WriteLine($"Error: Could not update rating for book with ID {bookId}.");
        }

        return Task.CompletedTask;
    }
}
