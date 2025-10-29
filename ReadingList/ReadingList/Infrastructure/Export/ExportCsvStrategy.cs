using ReadingList.App;
using ReadingList.Domain;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;

namespace ReadingList.Infrastructure;

public class ExportCsvStrategy<T>(IFileSystem _fileSystem) : IExportStrategy<T>
{
    public ExportType ExportType => ExportType.Csv;

    public async Task<Result> ExportAsync(
        IEnumerable<T> items,
        string path,
        bool shouldOverwrite = false,
        CancellationToken cancellationToken = default)
    {
        var result = new Result();

        try
        {
            if (items == null)
            {
                result.AddError("Items collection cannot be null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                result.AddError("Path cannot be null or empty.");
                return result;
            }

            var directory = _fileSystem.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                _fileSystem.Directory.CreateDirectory(directory);
            }

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                      .Where(p => p.CanRead)
                                      .ToArray();

            await using var stream = _fileSystem.File.Open(path,
                                                           shouldOverwrite ? FileMode.Create : FileMode.Append,
                                                           FileAccess.Write);
            await using var writer = new StreamWriter(stream, Encoding.UTF8);

            if (stream.Length == 0)
            {
                await writer.WriteLineAsync(string.Join(",", properties.Select(p => p.Name)));
            }

            foreach (var item in items)
            {
                //await Task.Delay(1000, cancellationToken); // Simulate a long-running operation
                if (cancellationToken.IsCancellationRequested)
                {
                    result.AddError("Export operation was cancelled.");
                    return result;
                }

                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item);
                    return CsvHelper.EscapeCsv(value?.ToString() ?? string.Empty);
                });

                await writer.WriteLineAsync(string.Join(",", values));
            }

            await writer.FlushAsync();
        }
        catch (Exception ex)
        {
            result.AddError(ex.Message);
        }

        return result;
    }
}
