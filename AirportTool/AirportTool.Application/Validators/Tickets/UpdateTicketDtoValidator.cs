namespace AirportTool.Application;

public class UpdateTicketDtoValidator : IValidator<UpdateTicketDto>
{
    public Result Validate(UpdateTicketDto instance)
    {
        var result = new Result();
        ValidateSeatInventory(instance, result);
        return result;
    }

    private void ValidateSeatInventory(UpdateTicketDto instance, Result result)
    {
        if (instance.SeatInventory <= 0)
        {
            var error = new Error
            {
                Message = "Seat inventory must be positive.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }
}
