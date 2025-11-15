using ReadingList.Domain;

namespace ReadingList_Tests;

public static class BookHelper
{
    public static Book GetValidBook() => new Book()
    {
        Id = 2,
        Title = "Valid Book",
        Author = "Valid Author",
        Year = 2021,
        Pages = 250,
        Genre = Genre.History,
        Finished = false,
        Rating = 4.5
    };
}
