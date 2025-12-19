using AirportTool.Domain;

namespace AirportTool.Application;

public interface IFlightScheduleRepository : IRepository<FlightSchedule, int>
{
    Task<FlightSchedule?> GetByFlightAndDepartureAsync(
        int flightId, DateTime departureUtc, CancellationToken cancellationToken);

    Task<GetFlightScheduleDto?> GetDtoByIdAsync(
        int flightScheduleId, CancellationToken cancellationToken);

    Task<PagedResult<FlightScheduleSearchDto>> GetFilteredFlightSchedulesAsync(
        FlightScheduleFilterDto filter, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyFlightStatsDto>> GetDailyStatsAsync(
        DateOnly startUtc, DateOnly endUtc, CancellationToken cancellationToken);
}
