namespace AirportTool.Application;

public class UpsertFlightScheduleDto
{
    public int FlightId { get; set; }

    public string AirlineIata { get; set; } = null!;

    public string OriginIata { get; set; } = null!;

    public string DestinationIata { get; set; } = null!;

    public DateTime ScheduledDepartureUtc { get; set; }

    public DateTime ScheduledArrivalUtc { get; set; }

    public string GateCode { get; set; } = null!;

    public string AssignedAircraftTail { get; set; } = null!;
}
