namespace AirportTool.Application;

public class GetAircraftDto
{
    public int AircraftId { get; set; }

    public string TailNumber { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int SeatCapacity { get; set; }

    public string? OwnedByAirlineIataCode { get; set; }

    public string? OwnedByAirlineName { get; set; }
}
