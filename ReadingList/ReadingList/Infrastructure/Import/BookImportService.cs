using Microsoft.Extensions.Logging;
using ReadingList.App;
using ReadingList.Domain;
using System.Collections.Concurrent;
using System.IO.Abstractions;

namespace ReadingList.Infrastructure;

public class BookImportService(IRepository<Book, int> _repository,
                               IBookParser _csvBookParser,
                               ILogger<BookImportService> _logger,
                               IFileSystem _fileSystem) : IImportService
{
    public async Task<Result<ImportReport>> ImportAsync(IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        var errors = new ConcurrentBag<string>();
        var fileReports = new ConcurrentBag<FileImportReport>();

        try
        {
            await Parallel.ForEachAsync(paths, cancellationToken, async (path, token) =>
            {
                var reportResult = await ImportFileAsync(path, token);
                reportResult.Errors.ForEach(e => errors.Add(e));
                fileReports.Add(reportResult.Value!);
            });
        }
        catch (OperationCanceledException ex)
        {
            var message = "Import operation was cancelled";
            _logger.LogWarning(ex, message);
            errors.Add(message);
        }

        var files = fileReports.ToList();
        var importReport = new ImportReport(files,
                                            files.Sum(r => r.Imported),
                                            files.Sum(r => r.Duplicates),
                                            files.Sum(r => r.Malformed));
        return new Result<ImportReport>(importReport, errors.ToList());
    }

    private async Task<Result<FileImportReport>> ImportFileAsync(string path, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        int imported = 0, duplicates = 0, malformed = 0;
        var fileName = _fileSystem.Path.GetFileName(path);

        if (!_fileSystem.File.Exists(path))
        {
            var message = $"File not found: {path}";
            _logger.LogError(message);
            errors.Add(message);
            var fileImportReport = new FileImportReport(fileName, 0, 0, 0);
            return new Result<FileImportReport>(fileImportReport, errors);
        }

        string[] lines;
        try
        {
            lines = await _fileSystem.File.ReadAllLinesAsync(path, cancellationToken);
        }
        catch(OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"Error reading file {fileName}";
            _logger.LogError(ex, message);
            errors.Add(message);
            var fileImportReport = new FileImportReport(fileName, 0, 0, 0);
            return new Result<FileImportReport>(fileImportReport, errors);
        }

        foreach (var (line, index) in lines.Skip(1).Select((l, i) => (l, i + 2)))
        {
            //await Task.Delay(1000, ct); // Simulate processing time
            cancellationToken.ThrowIfCancellationRequested();

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