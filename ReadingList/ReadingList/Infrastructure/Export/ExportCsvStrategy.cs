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

    public async Task<Result> ExportAsync(
        IEnumerable<T> items,
        string path,
        bool shouldOverwrite = false,
        CancellationToken cancellationToken = default)
    {
        var result = new Result();
        string? tmpPath = null;

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

            if (shouldOverwrite)
            {
                var fileName = _fileSystem.Path.GetFileName(path);
                var tempName = $"{fileName}.{Guid.NewGuid():N}.tmp";
                tmpPath = string.IsNullOrEmpty(directory) ? tempName : _fileSystem.Path.Combine(directory, tempName);

                await using (var temporary = _fileSystem.File.Open(tmpPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await temporary.WriteAsync(headerBytes, cancellationToken);
                    await temporary.WriteAsync(NewLine, cancellationToken);

                    foreach (var item in items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var line = string.Join(",", _props.Select(prop =>
                        {
                            var value = prop.GetValue(item);
                            return CsvHelper.EscapeCsv(value?.ToString() ?? string.Empty);
                        }));

                        var lineBytes = Encoding.UTF8.GetBytes(line);
                        await temporary.WriteAsync(lineBytes, cancellationToken);
                        await temporary.WriteAsync(NewLine, cancellationToken);
                    }

                    await temporary.FlushAsync(cancellationToken);
                }

                if (_fileSystem.File.Exists(path))
                {
                    _fileSystem.File.Replace(tmpPath, path, destinationBackupFileName: null);
                }
                else
                {
                    _fileSystem.File.Move(tmpPath, path);
                }

                return result; 
            }
            else
            {
                await using var stream = _fileSystem.File.Open(path, FileMode.Append, FileAccess.Write, FileShare.None);

                if (stream.Length == 0)
                {
                    await stream.WriteAsync(headerBytes, cancellationToken);
                    await stream.WriteAsync(NewLine, cancellationToken);
                }

                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var line = string.Join(",", _props.Select(p =>
                    {
                        var v = p.GetValue(item);
                        return CsvHelper.EscapeCsv(v?.ToString() ?? string.Empty);
                    }));

                    var lineBytes = Encoding.UTF8.GetBytes(line);
                    await stream.WriteAsync(lineBytes, cancellationToken);
                    await stream.WriteAsync(NewLine, cancellationToken);
                }

                await stream.FlushAsync(cancellationToken);
                return result; 
            }
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
            if (tmpPath is not null && _fileSystem.File.Exists(tmpPath))
            {
                try { _fileSystem.File.Delete(tmpPath); } catch { }
            }
        }
    }
}
