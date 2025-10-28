namespace ReadingList.App;

public static class Constants
{
    public const string LoggingFilePath = "Logs/log.txt";

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

}
