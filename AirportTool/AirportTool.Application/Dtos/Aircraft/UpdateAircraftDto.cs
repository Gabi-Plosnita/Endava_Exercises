namespace AirportTool.Application;

public class UpdateAircraftDto
{
    public string TailNumber { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int SeatCapacity { get; set; }

    public string? OwnedByAirlineIataCode { get; set; }
}
