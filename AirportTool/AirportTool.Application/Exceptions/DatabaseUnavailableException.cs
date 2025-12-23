namespace AirportTool.Application;

public sealed class DatabaseUnavailableException : DatabaseException
{
    public DatabaseUnavailableException(string message)
        : base(message)
    {
    }

    public DatabaseUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
