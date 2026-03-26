namespace AirportTool.Application;

public sealed class DatabaseConcurrencyException : DatabaseException
{
    public DatabaseConcurrencyException(string message)
        : base(message)
    {
    }

    public DatabaseConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
