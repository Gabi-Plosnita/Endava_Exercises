namespace ReadingList.Domain;

public static class BookExtensions
{
    public static int FinishedCount(this IEnumerable<Book> books)
    {
        return books.Count(b => b.Finished);
    }
    
    public static double AverageRating(this IEnumerable<Book> books)
    {
        return books.Any() ? books.Average(b => b.Rating) : 0.0;
    }

    public static Dictionary<Genre, int> PagesByGenre(this IEnumerable<Book> books)
    {
        return books.GroupBy(b => b.Genre)
                    .ToDictionary(g => g.Key, g => g.Sum(b => b.Pages));
    }

    public static IEnumerable<string> TopAuthorsByBookCount(this IEnumerable<Book> books, int topN = 3)
    {
        return books.GroupBy(b => b.Author)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(topN)
                    .Select(g => g.Key);
    }

    public static IEnumerable<Book> ByAuthor(this IEnumerable<Book> books, string author)
    {
        return books.Where(b => b.Author.Equals(author, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<Book> Finished(this IEnumerable<Book> books)
    {
        return books.Where(b => b.Finished);
    }

    public static IEnumerable<Book> TopRated(this IEnumerable<Book> books, int topN)
    {
        return books.OrderByDescending(b => b.Rating)
                    .ThenBy(b => b.Title)
                    .Take(topN);
    }
}
