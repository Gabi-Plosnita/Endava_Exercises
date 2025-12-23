namespace AirportTool.Application;

public sealed class DatabaseDeadlockException : DatabaseException
{
    public DatabaseDeadlockException(string message)
        : base(message)
    {
    }

    public DatabaseDeadlockException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
