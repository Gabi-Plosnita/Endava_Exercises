using ReadingList.Domain;

namespace ReadingList.App;

public class ImportCommand(IImportService _importService) : ICommand<ImportReport>
{
    public string Keyword => Constants.ImportCommandKeyword;

    public string Summary => Constants.ImportCommandSummary;

    public async Task<Result<ImportReport>> ExecuteAsync(string[] args, CancellationToken ct)
    {
        var result = await _importService.ImportAsync(args, ct); 
        return result;
    }
}
