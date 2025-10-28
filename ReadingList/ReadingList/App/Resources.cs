namespace ReadingList.App;

public static class Resources
{
    public const string LoggingFilePath = "../../../Logs/log.txt";

    // Commands //
    public const string ExistCommandKeyword = "exit";
    public const string HelpCommandKeyword = "help";
    public const string HelpCommandSummary = "help — display all available commands.";
    public const string ImportCommandKeyword = "import";
    public const string ImportCommandSummary = "import <file1.csv> [file2.csv ...] — imports one or more CSV files in parallel.";
    public const string ListAllCommandKeyword = "list_all";
    public const string ListAllCommandSummary = "list_all — lists all books.";
    public const string FilterFinishedCommandKeyword = "filter_finished";
    public const string FilterFinishedCommandSummary = "filter_finished — lists all finished books.";
    public const string TopRatedCommandKeyword = "top_rated";
    public const string TopRatedCommandSummary = "top_rated <N> — lists the top N rated books.";
    public const string ByAuthorCommandKeyword = "by_author";
    public const string ByAuthorCommandSummary = "by_author <author_name> — lists all books by the specified author.";
    public const string StatsCommandKeyword = "stats";
    public const string StatsCommandSummary = "stats - show total books, finished count, average rating, pages by genre, and top 3 authors by book count.";
    public const string MarkFinishedCommandKeyword = "mark_finished";
    public const string MarkFinishedCommandSummary = "mark_finished <book_id> — marks the book with the specified ID as finished.";
}
