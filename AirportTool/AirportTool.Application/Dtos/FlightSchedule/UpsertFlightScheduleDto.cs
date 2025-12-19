using AirportTool.Domain;

namespace AirportTool.Application;

public class UpsertFlightScheduleDto
{
    public int FlightId { get; set; }

    public DateTime ScheduledDepartureUtc { get; set; }

    public DateTime ScheduledArrivalUtc { get; set; }

    public string? GateCode { get; set; } = null!;

    public string? AssignedAircraftTail { get; set; } = null!;

    public FlightScheduleStatus Status { get; set; }
}
