namespace AirportTool.Application;

public class ScheduleConflictDto
{
    public int FlightScheduleId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public string AirportIata { get; set; } = null!;

    public DateTime ScheduledDepartureUtc { get; set; }

    public DateTime ScheduledArrivalUtc { get; set; }
}
