using ReadingList.Domain;

namespace ReadingList.App;

public class ImportCommand(IImportService _importService) : ICommand
{
    public string Keyword => Resources.ImportCommandKeyword;

    public string Summary => Resources.ImportCommandSummary;

    public async Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var argumentsValidation = ValidateArgs(args);
        if (argumentsValidation.IsFailure)
        {
            Console.WriteLine(argumentsValidation);
            return;
        }

        var result = await _importService.ImportAsync(args, ct);
        if(result.IsFailure)
        {
            Console.WriteLine(result);
            return;
        }

        if (result.Value != null)
        {
            Console.WriteLine(result.Value);
            Console.WriteLine("Import completed successfully.");
        }
    }

    private Result ValidateArgs(string[] args)
    {
        var result = new Result();
        if (args.Length == 0)
        {
            result.AddError("No import file specified.");
        }
        return result;
    }
}
