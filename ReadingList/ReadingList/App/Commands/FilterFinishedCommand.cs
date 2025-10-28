
using ReadingList.Domain;

namespace ReadingList.App;

public class FilterFinishedCommand(IRepository<Book, int> _repository) : ICommand
{
    public string Keyword => Constants.FilterFinishedCommandKeyword;

    public string Summary => Constants.FilterFinishedCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var finishedBooks = _repository.All()
                                       .Where(book => book.Finished)
                                       .ToList();

        if (finishedBooks.Count == 0)
        {
            Console.WriteLine("No finished books found.");
        }
        else
        {
            foreach (var book in finishedBooks)
            {
                Console.WriteLine(book);
            }
        }
        return Task.CompletedTask;
    }
}
