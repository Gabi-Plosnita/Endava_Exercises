using System.Text.RegularExpressions;

namespace AirportTool.Application;

public class UpdateFlightDtoValidator : BaseValidator, IValidator<UpdateFlightDto>
{
    private static readonly Regex FlightNumberRegex = new(@"^[A-Za-z]+[0-9]+$", RegexOptions.Compiled);

    public Result Validate(UpdateFlightDto dto)
    {
        var result = new Result();

        ValidateRequired(dto.FlightNumber, nameof(dto.FlightNumber), result);
        ValidateRequired(dto.AirlineIataCode, nameof(dto.AirlineIataCode), result);
        ValidateRequired(dto.OriginAirportIataCode, nameof(dto.OriginAirportIataCode), result);
        ValidateRequired(dto.DestinationAirportIataCode, nameof(dto.DestinationAirportIataCode), result);

        if (!string.IsNullOrWhiteSpace(dto.FlightNumber) && !FlightNumberRegex.IsMatch(dto.FlightNumber))
        {
            result.AddError(new Error
            {
                Message = "FlightNumber must be letters followed by numbers.",
                Type = ErrorType.Validation
            });
        }

        if (!string.IsNullOrWhiteSpace(dto.OriginAirportIataCode)
            && !string.IsNullOrWhiteSpace(dto.DestinationAirportIataCode)
            && dto.OriginAirportIataCode == dto.DestinationAirportIataCode)
        {
            result.AddError(new Error
            {
                Message = "Origin and Destination airports must be different.",
                Type = ErrorType.Validation
            });
        }

        return result;
    }
}
