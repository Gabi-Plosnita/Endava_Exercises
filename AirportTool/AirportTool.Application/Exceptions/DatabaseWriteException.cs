namespace AirportTool.Application;

public sealed class DatabaseWriteException : DatabaseException
{
    public DatabaseWriteException(string message)
        : base(message)
    {
    }

    public DatabaseWriteException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
