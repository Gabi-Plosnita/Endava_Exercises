namespace AirportTool.Application;

public class UpdateGateDtoValidator : BaseValidator, IValidator<UpdateGateDto>
{
    public Result Validate(UpdateGateDto dto)
    {
        var result = new Result();

        ValidateRequired(dto.AirportIataCode, nameof(dto.AirportIataCode), result);
        ValidateRequired(dto.GateCode, nameof(dto.GateCode), result);

        return result;
    }
}
