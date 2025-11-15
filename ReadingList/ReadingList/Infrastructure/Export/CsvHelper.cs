namespace ReadingList.Infrastructure;

public static class CsvHelper
{
    public static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            value = "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
