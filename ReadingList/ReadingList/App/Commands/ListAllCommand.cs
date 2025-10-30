namespace ReadingList.App;

public class ListAllCommand(IBookService _bookService) : ICommand
{
    public string Keyword => Resources.ListAllCommandKeyword;

    public string Summary => Resources.ListAllCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var books = _bookService.GetAll();

        if (!books.Any())
        {
            Console.WriteLine("No books found.");
            return Task.CompletedTask;
        }

        foreach (var book in books)
        {
            Console.WriteLine(book);
        }

        return Task.CompletedTask;
    }
}
