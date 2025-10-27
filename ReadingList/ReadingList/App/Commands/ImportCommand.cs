using ReadingList.Domain;

namespace ReadingList.App;

public class ImportCommand(IImportService _importService) : ICommand
{
    public string Keyword => Constants.ImportCommandKeyword;

    public string Summary => Constants.ImportCommandSummary;

    public async Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var result = await _importService.ImportAsync(args, ct);
        if (result.IsSuccessful)
        {
            Console.WriteLine("Import completed successfully.");
            if (result.Value is not null)
            {
                Console.WriteLine(result.Value);
            }
        }
        else
        {
            Console.WriteLine($"Import failed! {result}");
        }
    }
}
