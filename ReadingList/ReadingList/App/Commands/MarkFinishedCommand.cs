using ReadingList.Domain;

namespace ReadingList.App;

public class MarkFinishedCommand(IRepository<Book, int> _repository) : ICommand
{
    public string Keyword => Resources.MarkFinishedCommandKeyword;

    public string Summary => Resources.MarkFinishedCommandSummary;

    public Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        if(args.Length < 1)
        {
            Console.WriteLine("Error: No book ID provided.");
            return Task.CompletedTask;
        }

        if(!int.TryParse(args[0], out int bookId))
        {
            Console.WriteLine("Error: Invalid book ID format.");
            return Task.CompletedTask;
        }

        if(!_repository.TryGet(bookId, out var foundBook) || foundBook == null)
        {
            Console.WriteLine($"Error: No book found with ID {bookId}.");
            return Task.CompletedTask;
        }

        foundBook.Finished = true;
        var updateSuccess = _repository.Update(foundBook);
        if(updateSuccess)
        {
            Console.WriteLine($"Book ID {bookId} marked as finished.");
        }
        else
        {
            Console.WriteLine($"Error: Failed to update book ID {bookId}.");
        }

        return Task.CompletedTask;
    }
}
