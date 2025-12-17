using AirportTool.Domain;

namespace AirportTool.Application;

public interface IFlightScheduleRepository : IRepository<FlightSchedule, int>
{
    Task<FlightSchedule?> GetByFlightAndDepartureAsync(
        int flightId, DateTime departureUtc, CancellationToken cancellationToken);

    Task<FlightScheduleDetailsDto?> GetFlightScheduleDetailsAsync(
        int flightScheduleId, CancellationToken cancellationToken);

    Task<PagedResult<FlightScheduleSearchDto>> GetFilteredFlightSchedulesAsync(
        FlightFilterDto filter, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyFlightStatsDto>> GetDailyStatsAsync(
        DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken);
}
