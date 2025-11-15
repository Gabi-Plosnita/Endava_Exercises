namespace ReadingList.Domain;

public class Book
{
    public required int Id { get; set; }
    public required string Title { get; set; } = string.Empty;
    public required string Author { get; set; } = string.Empty;
    public required int Year { get; set; }
    public required int Pages { get; set; }
    public required Genre Genre { get; set; }
    public required bool Finished { get; set; }
    public required double Rating { get; set; }

    public Result Validate()
    {
        var result = new Result();
        if (Id <= 0)
        {
            result.AddError("Id must be grater than 0");
        }
        if (string.IsNullOrWhiteSpace(Title))
        {
            result.AddError("Title is required");
        }
        if (string.IsNullOrWhiteSpace(Author))
        {
            result.AddError("Author is required");
        }
        if (Year <= 0)
        {
            result.AddError("Year must be greater than 0");
        }
        if (Pages <= 0)
        {
            result.AddError("Pages must be greater than 0");
        }
        if (Rating < 0 || Rating > 5)
        {
            result.AddError("Rating must be between 0 and 5");
        }
        return result;
    }

    public void CopyFrom(Book book)
    {
        Title = book.Title;
        Author = book.Author;
        Year = book.Year;
        Pages = book.Pages;
        Genre = book.Genre;
        Finished = book.Finished;
        Rating = book.Rating;
    }

    public override string ToString()
    {
        return $"{Id}: {Title} by {Author} ({Year}) - {Pages} pages - Genre: {Genre} - Finished: {Finished} - Rating: {Rating}";
    }
}
