using AirportTool.Domain;

namespace AirportTool.Application;

public interface IFlightScheduleRepository : IRepository<FlightSchedule, int>
{
    Task<FlightScheduleDetailsDto?> GetFlightScheduleDetailsAsync(
        int flightScheduleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FlightScheduleSearchDto>> GetFilteredFlightSchedulesAsync(
        FlightFilterDto filter, CancellationToken cancellationToken = default);
}
