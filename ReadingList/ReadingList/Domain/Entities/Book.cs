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
}
