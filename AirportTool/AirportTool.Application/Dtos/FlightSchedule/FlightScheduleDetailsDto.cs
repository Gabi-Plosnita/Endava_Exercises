using AirportTool.Domain;

namespace AirportTool.Application;

public class FlightScheduleDetailsDto
{
    public int FlightScheduleId { get; set; }
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = null!;

    public string AirlineIata { get; set; } = null!;
    public string AirlineName { get; set; } = null!;

    public string OriginIata { get; set; } = null!;
    public string OriginName { get; set; } = null!;

    public string DestinationIata { get; set; } = null!;
    public string DestinationName { get; set; } = null!;

    public DateTime ScheduledDepartureUtc { get; set; }
    public DateTime ScheduledArrivalUtc { get; set; }

    public FlightScheduleStatus Status { get; set; }

    public string? AssignedAircraftTail { get; set; }
    public string? DefaultAircraftTail { get; set; }

    public string? GateCode { get; set; }
}
