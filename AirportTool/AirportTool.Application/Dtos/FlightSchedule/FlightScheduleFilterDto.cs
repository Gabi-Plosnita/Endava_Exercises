namespace AirportTool.Application;

public class FlightScheduleFilterDto : BaseFilterDto
{
    public string? OriginIata { get; init; }

    public string? DestinationIata { get; init; }

    public DateOnly? Date { get; init; }
}
