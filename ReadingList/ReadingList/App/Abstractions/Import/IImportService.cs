using ReadingList.Domain;

namespace ReadingList.App;

public interface IImportService
{
    Task<Result<ImportReport>> ImportAsync(IEnumerable<string> paths, CancellationToken cancellationToken);
}
