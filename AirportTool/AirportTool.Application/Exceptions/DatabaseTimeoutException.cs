namespace AirportTool.Application;

public sealed class DatabaseTimeoutException : DatabaseException
{
    public DatabaseTimeoutException(string message)
        : base(message)
    {
    }

    public DatabaseTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
