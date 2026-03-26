using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class AnnotationDtoValidator : IDtoValidator
{
    public Result Validate(object dto)
    {
        var result = new Result();

        if (dto is null)
        {
            result.AddError(new Error
            {
                Message = "DTO is null.",
                Type = ErrorType.Validation
            });
            return result;
        }

        var ctx = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            ctx,
            validationResults,
            validateAllProperties: true
        );

        foreach (var vr in validationResults)
        {
            var members = vr.MemberNames?.ToArray() ?? Array.Empty<string>();
            var message = vr.ErrorMessage ?? "Validation failed.";

            if (members.Length == 0)
            {
                result.AddError(new Error
                {
                    Message = message,
                    Type = ErrorType.Validation
                });
            }
            else
            {
                foreach (var member in members)
                {
                    result.AddError(new Error
                    {
                        Message = $"{member}: {message}",
                        Type = ErrorType.Validation
                    });
                }
            }
        }

        return result;
    }
}
