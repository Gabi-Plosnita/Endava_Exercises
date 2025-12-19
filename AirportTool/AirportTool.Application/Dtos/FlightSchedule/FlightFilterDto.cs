namespace AirportTool.Application;

public class FlightFilterDto : BaseFilterDto
{
    public string? OriginIata { get; init; }

    public string? DestinationIata { get; init; }

    public DateOnly? Date { get; init; }
}
