namespace AirportTool.Application;

public sealed class DatabaseDataTooLongException : DatabaseException
{
    public DatabaseDataTooLongException(string message)
        : base(message)
    {
    }

    public DatabaseDataTooLongException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
