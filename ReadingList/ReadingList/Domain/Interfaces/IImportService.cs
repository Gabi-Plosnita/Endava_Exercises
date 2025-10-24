namespace ReadingList.Domain;

public interface IImportService
{
    Task<Result<ImportReport>> ImportAsync(IEnumerable<string> paths, CancellationToken ct);
}
