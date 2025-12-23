using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class FlightScheduleFilterDto : BaseFilterDto
{
    [StringLength(3, MinimumLength = 3, ErrorMessage = "OriginIata must be exactly 3 characters.")]
    public string? OriginIata { get; init; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "DestinationIata must be exactly 3 characters.")]
    public string? DestinationIata { get; init; }

    public DateOnly? Date { get; init; }
}