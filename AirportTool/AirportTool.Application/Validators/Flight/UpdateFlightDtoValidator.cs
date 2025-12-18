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
        ValidateFlightNumberFormat(dto.FlightNumber, result);
        ValidateOriginAndDestinationAreDifferent(dto.OriginAirportIataCode, dto.DestinationAirportIataCode, result);

        return result;
    }

    private void ValidateFlightNumberFormat(string flightNumber, Result result)
    {
        if (!FlightNumberRegex.IsMatch(flightNumber))
        {
            var error = new Error
            {
                Message = "Flight number must start with letters followed by numbers (e.g., AA123).",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }

    private void ValidateOriginAndDestinationAreDifferent(string origin, string destination, Result result)
    {
        if (!string.IsNullOrWhiteSpace(origin) && !string.IsNullOrWhiteSpace(destination) && origin == destination)
        {
            var error = new Error
            {
                Message = "Origin and destination airports must be different.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }
}
