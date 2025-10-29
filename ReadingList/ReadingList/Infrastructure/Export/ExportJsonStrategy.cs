using ReadingList.App;
using ReadingList.Domain;
using System.IO.Abstractions;
using System.Text;
using System.Text.Json;

namespace ReadingList.Infrastructure;

public class ExportJsonStrategy<T>(IFileSystem _fileSystem) : IExportStrategy<T>
{
    public ExportType ExportType => ExportType.Json;

    public async Task<Result> ExportAsync(
        IEnumerable<T> items,
        string path,
        bool shouldOverwrite = false,
        CancellationToken cancellationToken = default)
    {
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
                    _fileSystem.Directory.CreateDirectory(directory);

                var options = new JsonSerializerOptions { WriteIndented = true };

                if (shouldOverwrite)
                {
                    var fileName = _fileSystem.Path.GetFileName(path);
                    var tempName = $"{fileName}.{Guid.NewGuid():N}.tmp";
                    tmpPath = string.IsNullOrEmpty(directory)
                        ? tempName
                        : _fileSystem.Path.Combine(directory, tempName);

                    await using (var tmpStream = _fileSystem.File.Open(tmpPath, FileMode.CreateNew, FileAccess.Write))
                    {
                        await JsonSerializer.SerializeAsync(tmpStream, items, options, cancellationToken);
                        await tmpStream.FlushAsync(cancellationToken);
                    }

                    if (_fileSystem.File.Exists(path))
                        _fileSystem.File.Replace(tmpPath, path, destinationBackupFileName: null);
                    else
                        _fileSystem.File.Move(tmpPath, path);
                }
                else
                {
                    await using var stream = _fileSystem.File.Open(path, FileMode.Append, FileAccess.Write);

                    if (stream.Length > 0)
                    {
                        var newLine = Encoding.UTF8.GetBytes(Environment.NewLine);
                        await stream.WriteAsync(newLine, cancellationToken);
                    }

                    await JsonSerializer.SerializeAsync(stream, items, options, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                return result; 
            }
            catch (OperationCanceledException)
            {
                result.AddError("Export operation was canceled.");
                return result;
            }
            catch (Exception ex)
            {
                result.AddError($"{ex.GetType().Name}: {ex.Message}");
                return result;
            }
            finally
            {
                if (tmpPath is not null && _fileSystem.File.Exists(tmpPath))
                {
                    try { _fileSystem.File.Delete(tmpPath); } catch {}
                }
            }
        }
    }
}