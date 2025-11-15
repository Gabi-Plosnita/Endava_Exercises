namespace ReadingList.App;

public class FilterFinishedCommand(IBookService _bookService) : ICommand
{
    public string Keyword => Constants.FilterFinishedCommandKeyword;

    public string Summary => Constants.FilterFinishedCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var books = _bookService.GetFinished(true);

        if (!books.Any())
        {
            Console.WriteLine("No finished books found.");
            return Task.CompletedTask;
        }

        foreach (var book in books)
        {
            Console.WriteLine(book);
        }

        return Task.CompletedTask;
    }
}
