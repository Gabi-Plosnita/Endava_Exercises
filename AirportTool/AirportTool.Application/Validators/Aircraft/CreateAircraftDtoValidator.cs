namespace AirportTool.Application;

public class CreateAircraftDtoValidator : BaseValidator, IValidator<CreateAircraftDto>
{
    public Result Validate(CreateAircraftDto dto)
    {
        var result = new Result();

        ValidateRequired(dto.TailNumber, nameof(dto.TailNumber), result);
        ValidateRequired(dto.Model, nameof(dto.Model), result);
        ValidateSeatCapacityIsPositive(dto.SeatCapacity, result);

        return result;
    }

    private void ValidateSeatCapacityIsPositive(int seatCapacity, Result result)
    {
        if (seatCapacity <= 0)
        {
            var error = new Error
            {
                Message = "Seat capacity must be a positive integer.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }
}
