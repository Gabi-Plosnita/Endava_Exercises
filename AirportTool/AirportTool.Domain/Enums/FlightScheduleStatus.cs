namespace AirportTool.Domain;

public enum FlightScheduleStatus : byte
{
    Planned = 0,
    Boarding = 1,
    Departed = 2,
    Cancelled = 3,
    Delayed = 4
}
