using AirportTool.Domain;

namespace AirportTool.Application;

public interface IFlightRepository : IRepository<Flight, int>
{
    Task<Flight?> GetFlightByAirlineAndFlightNumberAsync(
        string airlineIataCode, string flightNumber, CancellationToken cancellationToken = default);

    Task<Flight> GetByAirlineIdAndFlightNumberAsync(
        int airlineId, string flightNumber, CancellationToken cancellationToken = default);

    Task<bool> HasAnyFlightSchedulesAsync(int flightId, CancellationToken cancellationToken = default);
}
