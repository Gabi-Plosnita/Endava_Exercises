namespace AirportTool.Application;

public sealed class DatabaseNotNullException : DatabaseException
{
    public DatabaseNotNullException(string message)
        : base(message)
    {
    }

    public DatabaseNotNullException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
