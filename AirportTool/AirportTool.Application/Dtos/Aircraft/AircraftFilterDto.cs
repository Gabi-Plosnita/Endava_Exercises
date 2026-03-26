using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class AircraftFilterDto : BaseFilterDto
{
    [StringLength(2, MinimumLength = 2, ErrorMessage = "AirlineIataCode must be exactly 2 characters.")]
    public string? AirlineIataCode { get; set; }
}
