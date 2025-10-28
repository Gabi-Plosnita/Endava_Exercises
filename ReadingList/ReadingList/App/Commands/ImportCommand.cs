using ReadingList.Domain;

namespace ReadingList.App;

public class ImportCommand(IImportService _importService) : ICommand
{
    public string Keyword => Resources.ImportCommandKeyword;

    public string Summary => Resources.ImportCommandSummary;

    public async Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var result = await _importService.ImportAsync(args, ct);
        if (result.Value is not null)
        {
            Console.WriteLine(result.Value);
        }
        if (result.IsSuccessful)
        {
            Console.WriteLine("Import completed successfully.");
        }
        else
        {
            Console.WriteLine(result);
        }
    }
}
