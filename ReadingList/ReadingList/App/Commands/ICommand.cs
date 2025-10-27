using ReadingList.Domain;

namespace ReadingList.App;

public interface ICommand<T>
{
    string Keyword { get; }
    string Summary { get; }
    Task<Result<T>> ExecuteAsync(string[] args, CancellationToken ct);
}
