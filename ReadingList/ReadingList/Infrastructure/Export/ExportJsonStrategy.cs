using ReadingList.App;
using ReadingList.Domain;
using System.IO.Abstractions;
using System.Text.Json;

namespace ReadingList.Infrastructure;

public class ExportJsonStrategy<T>(IFileSystem _fileSystem) : IExportStrategy<T>
{
    public ExportType ExportType => ExportType.Json;

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

            var options = new JsonSerializerOptions { WriteIndented = true };

            var fileName = _fileSystem.Path.GetFileName(path);
            var tempName = $"{fileName}.{Guid.NewGuid():N}.tmp";
            temporaryPath = string.IsNullOrEmpty(directory)
                ? tempName
                : _fileSystem.Path.Combine(directory, tempName);

            await using (var tmpStream = _fileSystem.File.Open(temporaryPath, FileMode.CreateNew, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(tmpStream, items, options, cancellationToken);
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
        catch (Exception ex)
        {
            result.AddError($"{ex.GetType().Name}: {ex.Message}");
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