namespace AirportTool.Application;

internal class CreateBookingDtoValidator : BaseValidator, IValidator<CreateBookingDto>
{
    public Result Validate(CreateBookingDto instance)
    {
        var result = new Result();
        ValidateRequired(instance.PassengerFullName, nameof(instance.PassengerFullName), result);
        ValidateEmail(instance.PassengerEmail, result);
        ValidatePositive(instance.Quantity, nameof(instance.Quantity), result);
        return result;
    }
}
