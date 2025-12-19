namespace AirportTool.Application;

public class UpdateTicketDtoValidator : IValidator<UpdateTicketDto>
{
    public Result Validate(UpdateTicketDto instance)
    {
        var result = new Result();
        ValidateSeatInventory(instance.SeatInventory, result);
        return result;
    }

    private void ValidateSeatInventory(int seatInventory, Result result)
    {
        if (seatInventory < 0)
        {
            var error = new Error
            {
                Message = "Seat inventory cannot be negative.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }
}
