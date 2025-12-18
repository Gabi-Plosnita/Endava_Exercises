namespace AirportTool.Application;

public class CreateGateDtoValidator : BaseValidator, IValidator<CreateGateDto>
{
    public Result Validate(CreateGateDto dto)
    {
        var result = new Result();

        ValidateRequired(dto.AirportIataCode, nameof(dto.AirportIataCode), result);
        ValidateRequired(dto.Code, nameof(dto.Code), result);

        return result;
    }
}
