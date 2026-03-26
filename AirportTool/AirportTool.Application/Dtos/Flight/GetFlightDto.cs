namespace AirportTool.Application;

public class GetFlightDto
{
    public int FlightId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public string AirlineIata { get; set; } = null!;
    public string AirlineName { get; set; } = null!; 

    public string OriginIata { get; set; } = null!;
    public string OriginName { get; set; } = null!; 

    public string DestinationIata { get; set; } = null!;
    public string DestinationName { get; set; } = null!; 

    public string? DefaultAircraftTail { get; set; }

    public bool IsActive { get; set; }
}
