namespace AirportTool.Application;

public sealed class DatabaseUniqueConstraintException : DatabaseException
{
    public DatabaseUniqueConstraintException(string message)
        : base(message)
    {
    }

    public DatabaseUniqueConstraintException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
