namespace AirportTool.Application;

public class BaseValidator
{
    protected void ValidateRequired(string? value, string fieldName, Result result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.AddError(new Error
            {
                Message = $"{fieldName} is required.",
                Type = ErrorType.Validation
            });
        }
    }
}
