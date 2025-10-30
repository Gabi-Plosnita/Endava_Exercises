using ReadingList.Domain;

namespace ReadingList.App;

public class RateCommand(IBookService _bookService) : ICommand
{
    public string Keyword => Resources.RateCommandKeyword;

    public string Summary => Resources.RateCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var argumentsValidation = ValidateArguments(args);
        if (argumentsValidation.IsFailure)
        {
            Console.WriteLine(argumentsValidation);
            return Task.CompletedTask;
        }

        var (bookId, rating) = argumentsValidation.Value;
        var bookToUpdate = _bookService.GetById(bookId);
        if (bookToUpdate == null)
        {
            Console.WriteLine($"No book found with ID {bookId}.");
            return Task.CompletedTask;
        }

        bookToUpdate.Rating = rating;
        var updateResult = _bookService.Update(bookId, bookToUpdate);
        if (updateResult.IsSuccessful)
        {
            Console.WriteLine($"Successfully rated '{bookToUpdate.Title}' by {bookToUpdate.Author} — {rating}");
        }
        else
        {
            Console.WriteLine($"Error: Could not update rating for book with ID {bookId}.");
        }

        return Task.CompletedTask;
    }

    private Result<(int bookId, double raiting)> ValidateArguments(string[] args)
    {
        var result = new Result<(int bookId, double raiting)>();
        if (args.Length != 2)
        {
            result.AddError("Invalid number of arguments.");
            return result;
        }
        if (!int.TryParse(args[0], out int bookId))
        {
            result.AddError("Invalid book ID. It must be a number.");
        }
        if (!double.TryParse(args[1], out var rating) || rating < 0 || rating > 5)
        {
            result.AddError("Invalid rating. It must be a number between 0 and 5.");
        }

        if (result.IsFailure)
        {
            return result;
        }

        result.Value = (bookId, rating);
        return result;
    }
}
