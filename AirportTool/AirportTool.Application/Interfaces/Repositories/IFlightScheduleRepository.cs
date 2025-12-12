using AirportTool.Domain;

namespace AirportTool.Application;

public interface IFlightScheduleRepository : IRepository<FlightSchedule, int>
{
    Task<FlightSchedule?> GetByFlightAndDepartureAsync(
        int flightId, DateTime departureUtc, CancellationToken cancellationToken = default);

    Task<FlightScheduleDetailsDto?> GetFlightScheduleDetailsAsync(
        int flightScheduleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FlightScheduleSearchDto>> GetFilteredFlightSchedulesAsync(
        FlightFilterDto filter, CancellationToken cancellationToken = default);
}
