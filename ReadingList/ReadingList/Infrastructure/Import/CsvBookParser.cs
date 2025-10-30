using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using ReadingList.App;
using ReadingList.Domain;
using System.Globalization;

namespace ReadingList.Infrastructure;

public class CsvBookParser : IBookParser
{
    private readonly CsvConfiguration _configuration;

    public CsvBookParser()
    {
        _configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = false,
            IgnoreBlankLines = false,
            BadDataFound = null,       
            MissingFieldFound = null,
            DetectColumnCountChanges = false,
            TrimOptions = TrimOptions.None,
        };
    }

    public Result<Book> TryParse(string csvLine)
    {
        var result = new Result<Book>();
        if (string.IsNullOrWhiteSpace(csvLine))
        {
            result.AddError("Input line is null or empty.");
            return result;
        }

        try
        {
            using var reader = new StringReader(csvLine);
            using var csv = new CsvReader(reader, _configuration);

            var boolOpts = csv.Context.TypeConverterOptionsCache.GetOptions<bool>();
            boolOpts.BooleanTrueValues.AddRange(new[] { "true", "1", "yes", "y" });
            boolOpts.BooleanFalseValues.AddRange(new[] { "false", "0", "no", "n" });

            csv.Context.RegisterClassMap<BookMap>();

            if (!csv.Read())
            {
                result.AddError("No fields found in line.");
                return result;
            }

            var record = csv.GetRecord<Book>();
            var validationResult = record.Validate();
            if (validationResult.IsFailure)
            {
                result.AddError($"Validation failed for book: {record}. Errors: {validationResult}");
                return result;
            }

            return new Result<Book>(record);
        }
        catch (TypeConverterException ex)
        {
            result.AddError($"CSV parse error: {ex.Message}");
            return result;
        }
        catch (CsvHelperException ex)
        {
            result.AddError($"CSV parse error: {ex.Message}");
            return result;
        }
        catch (Exception ex)
        {
            result.AddError($"CSV parse error: {ex.Message}");
            return result;
        }
    }
}
