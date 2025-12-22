using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class UpdateFlightDto : IValidatableObject
{
    [Required(ErrorMessage = "FlightNumber is required.")]
    [RegularExpression(
        @"^[A-Za-z]+[0-9]+$",
        ErrorMessage = "Flight number must start with letters followed by numbers (e.g., AA123)."
    )]
    public string FlightNumber { get; set; } = null!;

    [Required(ErrorMessage = "AirlineIataCode is required.")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "AirlineIataCode must be exactly 2 characters.")]
    public string AirlineIataCode { get; set; } = null!;

    [Required(ErrorMessage = "OriginAirportIataCode is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "OriginAirportIataCode must be exactly 3 characters.")]
    public string OriginAirportIataCode { get; set; } = null!;

    [Required(ErrorMessage = "DestinationAirportIataCode is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "DestinationAirportIataCode must be exactly 3 characters.")]
    public string DestinationAirportIataCode { get; set; } = null!;

    public string? DefaultAircraftTail { get; set; }

    public bool IsActive { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(OriginAirportIataCode)
            && !string.IsNullOrWhiteSpace(DestinationAirportIataCode)
            && OriginAirportIataCode == DestinationAirportIataCode)
        {
            yield return new ValidationResult(
                "Origin and destination airports must be different.",
                new[] { nameof(OriginAirportIataCode), nameof(DestinationAirportIataCode) }
            );
        }
    }
}
