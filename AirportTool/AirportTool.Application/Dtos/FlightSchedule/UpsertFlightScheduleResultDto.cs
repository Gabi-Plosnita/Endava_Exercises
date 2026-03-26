namespace AirportTool.Application;

public class UpsertFlightScheduleResultDto
{
    public GetFlightScheduleDto? FlightSchedule { get; set; }

    public IReadOnlyList<ScheduleConflictDto> ScheduleConflicts { get; set; } = new List<ScheduleConflictDto>();
}
    