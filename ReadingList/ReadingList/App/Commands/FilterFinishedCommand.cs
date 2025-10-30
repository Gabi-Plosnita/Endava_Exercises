namespace ReadingList.App;

public class FilterFinishedCommand(IBookService _bookService) : ICommand
{
    public string Keyword => Resources.FilterFinishedCommandKeyword;

    public string Summary => Resources.FilterFinishedCommandSummary;

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
