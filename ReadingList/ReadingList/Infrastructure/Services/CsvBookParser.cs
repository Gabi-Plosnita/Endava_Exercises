using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using ReadingList.Domain;
using System.Globalization;

namespace ReadingList.Infrastructure;

public class CsvBookParser : ICsvBookParser
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
        if (string.IsNullOrWhiteSpace(csvLine))
        {
            return new Result<Book>(new[] { "Input line is null or empty." });
        }

        try
        {
            using var reader = new StringReader(csvLine);
            using var csv = new CsvReader(reader, _configuration);
            csv.Context.RegisterClassMap<BookMap>();

            if (!csv.Read())
                return new Result<Book>(new[] { "No fields found in line." });

            var record = csv.GetRecord<Book>();

            return new Result<Book>(record);
        }
        catch (TypeConverterException ex)
        {
            return new Result<Book>(new[] { $"CSV parse error: {ex.Message}" });
        }
        catch (CsvHelperException ex)
        {
            return new Result<Book>(new[] { $"CSV parse error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return new Result<Book>(new[] { $"CSV parse error: {ex.Message}" });
        }
    }
}
