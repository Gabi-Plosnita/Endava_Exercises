namespace AirportTool.Application;

public class UpsertFlightScheduleDtoValidator : IValidator<UpsertFlightScheduleDto>
{
    public Result Validate(UpsertFlightScheduleDto instance)
    {
        var result = new Result();
        if (instance.ScheduledArrivalUtc <= instance.ScheduledDepartureUtc)
        {
            var error = new Error
            {
                Message = "ScheduledArrivalUtc must be later than ScheduledDepartureUtc.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        if(instance.ScheduledArrivalUtc - instance.ScheduledDepartureUtc < TimeSpan.FromMinutes(10))
        {
            var error = new Error
            {
                Message = "The duration between ScheduledDepartureUtc and ScheduledArrivalUtc cannot be less than 10 minutes.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return result;
    }
}
