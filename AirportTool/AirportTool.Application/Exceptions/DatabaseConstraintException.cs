namespace AirportTool.Application;

public sealed class DatabaseConstraintException : DatabaseException
{
    public DatabaseConstraintException(string message)
        : base(message)
    {
    }

    public DatabaseConstraintException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
