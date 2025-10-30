using ReadingList.Domain;

namespace ReadingList.App;

public class ByAuthorCommand(IBookService _bookService) : ICommand
{
    public string Keyword => Resources.ByAuthorCommandKeyword;

    public string Summary => Resources.ByAuthorCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var argumentsValidation = ValidateArgs(args);
        if (argumentsValidation.IsFailure)
        {
            Console.WriteLine(argumentsValidation);
            return Task.CompletedTask;
        }

        var author = argumentsValidation.Value!;
        var booksResult = _bookService.GetBooksByAuthor(author);

        if(booksResult.IsFailure)
        {
            Console.WriteLine(booksResult);
            return Task.CompletedTask;
        }

        if(booksResult.Value == null || !booksResult.Value.Any())
        {
            Console.WriteLine($"No books found by author: {author}.");
            return Task.CompletedTask;
        }

        foreach (var book in booksResult.Value)
        {
            Console.WriteLine(book);
        }

        return Task.CompletedTask;
    }

    private Result<string> ValidateArgs(string[] args)
    {
        var result = new Result<string>();
        var author = string.Join(" ", args);

        if (string.IsNullOrWhiteSpace(author))
        {
            result.AddError("Author name cannot be empty.");
            return result;
        }

        result.Value = author;
        return result;
    }
}
