namespace AirportTool.Application;

public class Error
{
    public string Message { get; set; } = null!;

    public ErrorType Type { get; set; }
}

public enum ErrorType
{
    NotFound,
    Validation,
}