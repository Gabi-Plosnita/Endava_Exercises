using ReadingList.Domain;

namespace ReadingList.App;

public class TopRatedCommand(IBookService _bookService) : ICommand
{
    public string Keyword => Resources.TopRatedCommandKeyword;

    public string Summary => Resources.TopRatedCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var argumentValidation = ValidateArguments(args);
        if(argumentValidation.IsFailure)
        {
            Console.WriteLine(argumentValidation);
            return Task.CompletedTask;
        }

        var topRatedNumber = argumentValidation.Value;
        var topRatedBooks = _bookService.GetAll().TopRated(topRatedNumber);

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

    private Result<int> ValidateArguments(string[] args)
    {
        var result = new Result<int>();
        if(args.Length != 1)
        {
            result.AddError("Invalid number of arguments.");
            return result;
        }
        if(!int.TryParse(args[0], out int topRatedNumber) || topRatedNumber <= 0)
        {
            result.AddError("Number of top rated must be a positive integer.");
            return result;
        }
        result.Value = topRatedNumber;
        return result;
    }
}
