using ReadingList.Domain;

namespace ReadingList.App;

public class FilterFinishedCommand(IRepository<Book, int> _repository) : ICommand
{
    public string Keyword => Resources.FilterFinishedCommandKeyword;

    public string Summary => Resources.FilterFinishedCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var finishedBooks = _repository.All().Finished();

        if (!finishedBooks.Any())
        {
            Console.WriteLine("No finished books found.");
            return Task.CompletedTask;
        }

        foreach (var book in finishedBooks)
        {
            Console.WriteLine(book);
        }

        return Task.CompletedTask;
    }
}
