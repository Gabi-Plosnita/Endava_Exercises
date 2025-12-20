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

    protected void ValidatePositive(decimal value, string fieldName, Result result)
    {
        if (value <= 0)
        {
            result.AddError(new Error
            {
                Message = $"{fieldName} must be positive.",
                Type = ErrorType.Validation
            });
        }
    }

    protected void ValidateEmail(string email, Result result)
    {
        ValidateRequired(email, nameof(email), result);
        if(result.IsFailure)
        {
            return;
        }
        var isValid = email.Contains("@") && email.Contains(".");
        if (isValid)
        {
            var error = new Error
            {
                Message = $"{nameof(email)} is not a valid email address.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }
}
