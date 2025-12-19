namespace AirportTool.Application;

public class CreateTicketDtoValidator : BaseValidator, IValidator<CreateTicketDto>
{
    public Result Validate(CreateTicketDto instance)
    {
        var result = new Result();
        ValidateCurrency(instance.Currency, result);
        ValidatePositive(instance.BasePrice, nameof(instance.BasePrice), result);
        ValidatePositive(instance.Taxes, nameof(instance.Taxes), result);
        ValidatePositive(instance.SeatInventory, nameof(instance.SeatInventory), result);
        return result;
    }

    private void ValidateCurrency(string currency, Result result)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            var error = new Error
            {
                Message = "Currency must be a valid 3-letter ISO currency code.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }
}
