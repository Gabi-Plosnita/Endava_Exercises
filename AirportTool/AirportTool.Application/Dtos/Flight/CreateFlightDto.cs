namespace AirportTool.Application;

public class CreateFlightDto
{
    public string FlightNumber { get; set; } = null!;

    public string AirlineIataCode { get; set; } = null!;

    public string OriginAirportIataCode { get; set; } = null!;

    public string DestinationAirportIataCode { get; set; } = null!;

    public string? DefaultAircraftTail { get; set; }

    public bool IsActive { get; set; }
}
