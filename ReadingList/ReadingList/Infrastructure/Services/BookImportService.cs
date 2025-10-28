using Microsoft.Extensions.Logging;
using ReadingList.Domain;
using System.Collections.Concurrent;
using System.IO.Abstractions;

namespace ReadingList.Infrastructure;

public class BookImportService(IRepository<Book, int> _repository,
                               ICsvBookParser _csvBookParser,
                               ILogger<BookImportService> _logger,
                               IFileSystem _fileSystem) : IImportService
{
    public async Task<Result<ImportReport>> ImportAsync(IEnumerable<string> paths, CancellationToken ct)
    {
        var errors = new ConcurrentBag<string>();
        var fileReports = new ConcurrentBag<FileImportReport>();

        try
        {
            await Parallel.ForEachAsync(paths, ct, async (path, token) =>
            {
                var reportResult = await ImportFileAsync(path, token);
                reportResult.Errors.ForEach(e => errors.Add(e));
                fileReports.Add(reportResult.Value!);

            });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Import operation was cancelled.");
            errors.Add("Import operation was cancelled.");
        }

        var files = fileReports.ToList();
        var importReport = new ImportReport(files,
                                            files.Sum(r => r.Imported),
                                            files.Sum(r => r.Duplicates),
                                            files.Sum(r => r.Malformed));
        return new Result<ImportReport>(importReport, errors.ToList());
    }

    private async Task<Result<FileImportReport>> ImportFileAsync(string path, CancellationToken ct)
    {
        var errors = new List<string>();
        int imported = 0, duplicates = 0, malformed = 0;
        var fileName = _fileSystem.Path.GetFileName(path);

        if (!_fileSystem.File.Exists(path))
        {
            _logger.LogError("File not found: {Path}", path);
            errors.Add($"File not found: {path}");
            var fileImportReport = new FileImportReport(fileName, 0, 0, 0);
            return new Result<FileImportReport>(fileImportReport, errors);
        }

        string[] lines;
        try
        {
            lines = await _fileSystem.File.ReadAllLinesAsync(path, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {FileName}", fileName);
            errors.Add($"Error reading file {fileName}");
            var fileImportReport = new FileImportReport(fileName, 0, 0, 0);
            return new Result<FileImportReport>(fileImportReport, errors);
        }

        foreach (var (line, index) in lines.Skip(1).Select((l, i) => (l, i + 2)))
        {
            //await Task.Delay(1000, ct); // Simulate processing time

            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Import cancelled by user.");
                break;
            }

            var parseResult = _csvBookParser.TryParse(line);
            if(parseResult.IsFailure || parseResult.Value == null)
            {
                malformed++;
                var message = parseResult.IsFailure ? parseResult.ToString() : string.Empty;
                _logger.LogWarning("Parsing error at line {Line}. \n{Message}", index, message);
                continue;
            }

            var book = parseResult.Value;

            if (!_repository.Add(book))
            {
                duplicates++;
                _logger.LogWarning("Duplicate Id skipped at line {Line}: {BookId}", index, book.Id);
                continue;
            }

            imported++;
        }

        _logger.LogInformation(
            "[{File}] imported: {Imported}, duplicates: {Duplicates}, malformed: {Malformed}",
            fileName, imported, duplicates, malformed);

        var fileReport = new FileImportReport(fileName, imported, duplicates, malformed);
        return new Result<FileImportReport>(fileReport, errors);
    }
}