namespace ReadingList.Domain;

public interface IImportService
{
    Task<ImportReport> ImportAsync(IEnumerable<string> csvPaths, CancellationToken ct);
}
