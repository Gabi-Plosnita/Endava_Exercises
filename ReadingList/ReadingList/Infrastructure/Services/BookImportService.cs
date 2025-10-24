using Microsoft.Extensions.Logging;
using ReadingList.Domain;
using System.Collections.Concurrent;

namespace ReadingList.Infrastructure;

public class BookImportService(IRepository<Book, int> _repository,
                               ICsvBookParser _csvBookParser,   
                               ILogger<BookImportService> _logger) : IImportService
{
    public async Task<ImportReport> ImportAsync(IEnumerable<string> paths, CancellationToken ct)
    {
        var fileReports = new ConcurrentBag<FileImportReport>();

        await Parallel.ForEachAsync(paths, ct, async (path, token) =>
        {
            var report = await ImportFileAsync(path, token);
            fileReports.Add(report);
        });

        var files = fileReports.ToList();
        return new ImportReport(
            files,
            files.Sum(r => r.Imported),
            files.Sum(r => r.Duplicates),
            files.Sum(r => r.Malformed));
    }

    private async Task<FileImportReport> ImportFileAsync(string path, CancellationToken ct)
    {
        int imported = 0, duplicates = 0, malformed = 0;
        string fileName = Path.GetFileName(path);

        if (!File.Exists(path))
        {
            _logger.LogError("File not found: {Path}", path);
            return new FileImportReport(fileName, 0, 0, 0);
        }

        string[] lines;
        try
        {
            lines = await File.ReadAllLinesAsync(path, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {FileName}", fileName);
            return new FileImportReport(fileName, 0, 0, 0);
        }

        foreach (var (line, index) in lines.Skip(1).Select((l, i) => (l, i + 2)))
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Import cancelled by user.");
                break;
            }

            var result = _csvBookParser.TryParse(line);
            if (result.IsFailure)
            {
                malformed++;
                _logger.LogWarning("Failed to parse at line {Line}. {Result}", index, result);
                continue;
            }

            var book = result.Value;
            if(book == null)
            {
                malformed++;
                _logger.LogWarning("PaFailed to parse at line {Line}", index);
                continue;
            }

            if (!_repository.Add(book))
            {
                duplicates++;
                _logger.LogWarning("Duplicate Id skipped at line {Line}: {BookId}", index, book.Id);
                continue;
            }

            imported++;
        }

        _logger.LogInformation("[{File}] imported: {Imported}, duplicates: {Duplicates}, malformed: {Malformed}",
            fileName, imported, duplicates, malformed);

        return new FileImportReport(fileName, imported, duplicates, malformed);
    }
}