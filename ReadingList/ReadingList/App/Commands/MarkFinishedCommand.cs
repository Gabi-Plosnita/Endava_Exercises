using ReadingList.Domain;

namespace ReadingList.App;

public class MarkFinishedCommand(IBookService _bookService) : ICommand
{
    public string Keyword => Constants.MarkFinishedCommandKeyword;

    public string Summary => Constants.MarkFinishedCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var argumentValidation = ValidateArgs(args);
        if(argumentValidation.IsFailure)
        {
            Console.WriteLine(argumentValidation);
            return Task.CompletedTask;
        }

        var bookId = argumentValidation.Value;
        var bookToUpdate = _bookService.GetById(bookId);
        if (bookToUpdate == null)
        {
            Console.WriteLine($"No book found with id {bookId}.");
            return Task.CompletedTask;
        }

        bookToUpdate.Finished = true;
        var updateResult = _bookService.Update(bookId, bookToUpdate);
        if(updateResult.IsFailure)
        {
            Console.WriteLine(updateResult);
            return Task.CompletedTask;
        }

        Console.WriteLine($"Book with id {bookId} marked as finished.");
        return Task.CompletedTask;
    }

    private Result<int> ValidateArgs(string[] args)
    {
        var result = new Result<int>();
        if(args.Length != 1)
        {
            result.AddError("Invalid number of arguments.");
            return result;
        }
        if(!int.TryParse(args[0], out int bookId))
        {
            result.AddError("Invalid book ID format.");
            return result;
        }
        if(bookId < 0)
        {
            result.AddError("Id must be grater than 0.");
            return result;
        }
        result.Value = bookId;
        return result;
    }
}
