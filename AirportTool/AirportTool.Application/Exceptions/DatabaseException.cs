namespace AirportTool.Application;

public abstract class DatabaseException : ApplicationException
{
    protected DatabaseException(string message)
        : base(message)
    {
    }

    protected DatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
