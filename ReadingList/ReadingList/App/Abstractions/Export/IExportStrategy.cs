using ReadingList.Domain;

namespace ReadingList.App;

public interface IExportStrategy<T>
{
    ExportType ExportType { get; }
    Task<Result> ExportAsync(IEnumerable<T> items, string path, bool shouldOverride = false, CancellationToken cancellationToken = default);
}
