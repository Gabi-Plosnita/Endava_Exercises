using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class CreateAircraftDto
{
    [Required(ErrorMessage = "TailNumber is required.")]
    public string TailNumber { get; set; } = null!;

    [Required(ErrorMessage = "Model is required.")]
    public string Model { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Seat capacity must be a positive integer.")]
    public int SeatCapacity { get; set; }

    [StringLength(2, MinimumLength = 2, ErrorMessage = "OwnedByAirlineIataCode must be exactly 2 characters.")]
    public string? OwnedByAirlineIataCode { get; set; }
}
