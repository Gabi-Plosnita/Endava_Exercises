namespace ReadingList.App;

public interface ICommand
{
    string Keyword { get; }

    string Summary { get; }

    Task ExecuteAsync(string[] args, CancellationToken ct);
}