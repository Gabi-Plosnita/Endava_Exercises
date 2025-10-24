namespace ReadingList.Domain;

public interface IImportService
{
    Task<ImportReport> ImportAsync(IEnumerable<string> paths, CancellationToken ct);
}
