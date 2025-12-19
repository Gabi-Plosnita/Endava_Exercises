namespace AirportTool.Application;

public class BaseFilterDtoValidator : BaseValidator, IValidator<BaseFilterDto>
{
    public Result Validate(BaseFilterDto instance)
    {
        var result = new Result();
        if (instance.PageIndex < 0)
        {   
            result.AddError(new Error
            {
                Message = "PageIndex must be greater than or equal to 0.",
                Type = ErrorType.Validation
            });
        }
        if(instance.PageSize <= 0 || instance.PageSize > 100)
        {
            result.AddError(new Error
            {
                Message = "PageSize must be between 1 and 100",
                Type = ErrorType.Validation
            });
        }
        return result;
    }
}
