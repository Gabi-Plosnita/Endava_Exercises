namespace AirportTool.Application;

public class FlightFilterDto : PagedQueryDto
{
    public string? OriginIata { get; init; }
    public string? DestinationIata { get; init; }
    public DateOnly? Date { get; init; }
}
