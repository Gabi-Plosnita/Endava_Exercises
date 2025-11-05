using ReadingList.Domain;

namespace ReadingList.App;

public interface IExportService<T>
{
    Task<Result> ExportAsync(ExportType exportType, 
                             IEnumerable<T> items, 
                             string path, 
                             bool shouldOverwrite = false, 
                             CancellationToken cancellationToken = default);
}
