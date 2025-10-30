using ReadingList.App;
using ReadingList.Domain;

namespace ReadingList.Infrastructure;

public class ExportService<T> : IExportService<T>
{
    private readonly Dictionary<ExportType, IExportStrategy<T>> _strategies;

    public ExportService(IEnumerable<IExportStrategy<T>> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.ExportType);
    }

    public async Task<Result> ExportAsync(
        ExportType exportType,
        IEnumerable<T> items,
        string path,
        bool shouldOverwrite = false,
        CancellationToken cancellationToken = default)
    {
        if (!_strategies.TryGetValue(exportType, out var strategy))
        {
            var result = new Result();
            result.AddError($"No export strategy found for type '{exportType}'.");
            return result;
        }

        return await strategy.ExportAsync(items, path, shouldOverwrite, cancellationToken);
    }
}

