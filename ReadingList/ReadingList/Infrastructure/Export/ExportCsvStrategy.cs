using ReadingList.App;
using ReadingList.Domain;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;

namespace ReadingList.Infrastructure;

public class ExportCsvStrategy<T>(IFileSystem _fileSystem) : IExportStrategy<T>
{
    public ExportType ExportType => ExportType.Csv;

    private static readonly PropertyInfo[] _props = typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead)
        .ToArray();

    private static readonly byte[] NewLine = Encoding.UTF8.GetBytes(Environment.NewLine);

    public async Task<Result> ExportAsync(IEnumerable<T> items, string path, CancellationToken cancellationToken = default)
    {
        var result = new Result();
        string? temporaryPath = null;

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

            var header = string.Join(",", _props.Select(p => p.Name));
            var headerBytes = Encoding.UTF8.GetBytes(header);

            var fileName = _fileSystem.Path.GetFileName(path);
            var tempName = $"{fileName}.{Guid.NewGuid():N}.tmp";
            temporaryPath = string.IsNullOrEmpty(directory) ? tempName : _fileSystem.Path.Combine(directory, tempName);

            await using (var tmpStream = _fileSystem.File.Open(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await tmpStream.WriteAsync(headerBytes, cancellationToken);
                await tmpStream.WriteAsync(NewLine, cancellationToken);

                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var line = string.Join(",", _props.Select(prop =>
                    {
                        var value = prop.GetValue(item);
                        return CsvHelper.EscapeCsv(value?.ToString() ?? string.Empty);
                    }));

                    var lineBytes = Encoding.UTF8.GetBytes(line);
                    await tmpStream.WriteAsync(lineBytes, cancellationToken);
                    await tmpStream.WriteAsync(NewLine, cancellationToken);
                }

                await tmpStream.FlushAsync(cancellationToken);
            }

            if (_fileSystem.File.Exists(path))
            {
                _fileSystem.File.Replace(temporaryPath, path, destinationBackupFileName: null);
            }
            else
            {
                _fileSystem.File.Move(temporaryPath, path);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            result.AddError("Export operation was canceled.");
            return result;
        }
        catch (Exception)
        {
            result.AddError("Unexpected error during export!");
            return result;
        }
        finally
        {
            if (temporaryPath is not null && _fileSystem.File.Exists(temporaryPath))
            {
                try { _fileSystem.File.Delete(temporaryPath); } catch { }
            }
        }
    }
}