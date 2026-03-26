namespace AirportTool.Application;

public class Error
{
    public string Message { get; set; } = null!;

    public ErrorType Type { get; set; }

    public override string ToString() => $"{Type}: {Message}";
}

public enum ErrorType
{
    NotFound,
    Validation,
    Conflict,
    Unexpected,
}