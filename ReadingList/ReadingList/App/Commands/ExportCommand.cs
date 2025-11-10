
using ReadingList.Domain;

namespace ReadingList.App;

public class ExportCommand(IBookService _bookService, IExportService<Book> _exportService) : ICommand
{
    public string Keyword => Constants.ExportCommandKeyword;

    public string Summary => Constants.ExportCommandSummary;

    public async Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        var argumentsValidation = ValidateArguments(args);
        if(argumentsValidation.IsFailure)
        {
            Console.WriteLine(argumentsValidation);
            return;
        }

        Console.Write($"Do you want to overwrite the file? (y/n): ");
        string input = Console.ReadLine().Trim().ToLower();

        if(input != "y" && input != "n")
        {
            Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
            return;
        }

        bool shouldOverwrite = input == "y";
        if(!shouldOverwrite)
        {
            Console.WriteLine("Operation was cancelled");
            return;
        }

        var (exportType, filePath) = argumentsValidation.Value;
        var books = _bookService.GetAll();

        var exportResult = await _exportService.ExportAsync(exportType, books, filePath, shouldOverwrite: shouldOverwrite, ct);
        if(exportResult.IsFailure)
        {
            Console.WriteLine(exportResult);
            return;
        }

        Console.WriteLine($"Books exported successfully to {filePath}");
    }

    public Result<(ExportType type, string filePath)> ValidateArguments(string[] args)
    {
        var result = new Result<(ExportType type, string filePath)>();

        if (args.Length != 2)
        {
            result.AddError("Invalid number of arguments.");
            return result;
        }

        var extension = args[0].ToLower();
        if (extension is not ("csv" or "json"))
        {
            result.AddError("Invalid export type. Must be 'csv' or 'json'.");
            return result;
        }

        var exportType = args[0].ToLower() == "csv" ? ExportType.Csv : ExportType.Json;
        var filePath = args[1];
        var fileExtension = Path.GetExtension(filePath).ToLower().TrimStart('.');

        if (!string.Equals(extension,fileExtension))
        {
            result.AddError("Invalid file path. File extension should match the export type.");
            return result;
        }

        result.Value = (exportType, filePath);
        return result;
    }
}
