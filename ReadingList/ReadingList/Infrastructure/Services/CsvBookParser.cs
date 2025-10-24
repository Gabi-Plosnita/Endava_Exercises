using ReadingList.Domain;
using System.Globalization;
using System.Text;

public sealed class CsvBookParser : ICsvBookParser
{
    private const int IdIdx = 0;
    private const int TitleIdx = 1;
    private const int AuthorIdx = 2;
    private const int YearIdx = 3;
    private const int PagesIdx = 4;
    private const int GenreIdx = 5;
    private const int FinishedIdx = 6;
    private const int RatingIdx = 7;
    private const int ExpectedColumns = 8;

    public Result<Book> TryParse(string csvLine)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(csvLine))
        {
            errors.Add("Input line is null or empty.");
            return new Result<Book>(errors);
        }

        var fields = SplitCsvLine(csvLine);

        if (fields.Count != ExpectedColumns)
        {
            errors.Add($"Expected {ExpectedColumns} columns, but got {fields.Count}.");
            return new Result<Book>(errors);
        }

        // Id
        if (!int.TryParse(fields[IdIdx], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id <= 0)
        {
            errors.Add($"Invalid Id '{fields[IdIdx]}'. Id must be a positive integer.");
        }

        // Title
        var title = fields[TitleIdx]?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add("Title cannot be empty.");
        }

        // Author
        var author = fields[AuthorIdx]?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(author))
        {
            errors.Add("Author cannot be empty.");
        }

        // Year
        if (!int.TryParse(fields[YearIdx], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
        {
            errors.Add($"Invalid Year '{fields[YearIdx]}'.");
        }
        else if (year < 0 || year > DateTime.UtcNow.Year + 1)
        {
            errors.Add($"Unreasonable Year '{year}'.");
        }

        // Pages
        if (!int.TryParse(fields[PagesIdx], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pages) || pages <= 0)
        {
            errors.Add($"Invalid Pages '{fields[PagesIdx]}'. Pages must be a positive integer.");
        }

        // Genre
        var genreText = fields[GenreIdx]?.Trim() ?? string.Empty;
        var genre = ParseGenre(genreText, out var genreError);
        if (genreError is not null) errors.Add(genreError);

        // Finished
        var finishedText = fields[FinishedIdx]?.Trim() ?? string.Empty;
        if (!TryParseBoolFlexible(finishedText, out var finished))
        {
            errors.Add($"Invalid Finished value '{fields[FinishedIdx]}'. Use yes/no, y/n, true/false, or 1/0.");
        }

        // Rating
        if (!double.TryParse(fields[RatingIdx], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var rating))
        {
            errors.Add($"Invalid Rating '{fields[RatingIdx]}'. Use a number like 4.5 with '.' as the decimal separator.");
        }
        else if (rating < 0 || rating > 5)
        {
            errors.Add($"Rating '{rating}' is out of range. Expected 0..5.");
        }

        var book = new Book
        {
            Id = id,
            Title = title,
            Author = author,
            Year = year,
            Pages = pages,
            Genre = genre,
            Finished = finished,
            Rating = rating
        };

        return errors.Count == 0
            ? new Result<Book>(book)
            : new Result<Book>(book, errors);
    }

    private static Genre ParseGenre(string text, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Genre cannot be empty.";
            return Genre.Unknown;
        }

        var normalized = text.Replace(" ", "", StringComparison.Ordinal)
                             .Replace("_", "", StringComparison.Ordinal);

        if (Enum.TryParse(typeof(Genre), normalized, ignoreCase: true, out var parsed)
            && parsed is Genre g)
        {
            return g;
        }

        switch (normalized.ToLowerInvariant())
        {
            case "sci-fi":
            case "scifi":
            case "sciencefiction":
                return Genre.ScienceFiction;
            case "software":
                return Genre.Software;
            case "fantasy":
                return Genre.Fantasy;
            case "poetry":
                return Genre.Poetry;
            case "history":
                return Genre.History;
            case "biography":
                return Genre.Biography;
            case "selfhelp":
                return Genre.SelfHelp;
            case "philosophy":
                return Genre.Philosophy;
        }

        error = $"Unknown Genre '{text}'.";
        return Genre.Unknown;
    }

    private static bool TryParseBoolFlexible(string text, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var t = text.Trim().Trim('"').ToLowerInvariant();
        switch (t)
        {
            case "y":
            case "yes":
            case "true":
            case "1":
                value = true; return true;
            case "n":
            case "no":
            case "false":
            case "0":
                value = false; return true;
            default:
                return bool.TryParse(t, out value);
        }
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>(ExpectedColumns);
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++; 
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
