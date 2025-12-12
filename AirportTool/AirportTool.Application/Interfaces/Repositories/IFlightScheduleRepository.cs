using AirportTool.Domain;

namespace AirportTool.Application;

public interface IFlightScheduleRepository : IRepository<FlightSchedule, int>
{
    Task<IReadOnlyCollection<FlightScheduleSearchDto>> GetFilteredFlightSchedulesAsync(
        FlightFilterDto filter,
        CancellationToken cancellationToken = default);
}
