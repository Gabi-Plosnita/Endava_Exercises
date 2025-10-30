using ReadingList.Domain;

namespace ReadingList.App;

public interface IExportStrategy<T>
{
    ExportType ExportType { get; }
    Task<Result> ExportAsync(IEnumerable<T> items, string path, CancellationToken cancellationToken = default);
}
