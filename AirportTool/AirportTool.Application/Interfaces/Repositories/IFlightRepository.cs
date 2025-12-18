using AirportTool.Domain;

namespace AirportTool.Application;

public interface IFlightRepository : IRepository<Flight, int>
{
    Task<GetFlightDto?> GetDtoByIdAsync(int flightId, CancellationToken cancellationToken);

    Task<Flight?> GetFlightByAirlineAndFlightNumberAsync(
        string airlineIataCode, string flightNumber, CancellationToken cancellationToken);

    Task<Flight> GetByAirlineIdAndFlightNumberAsync(
        int airlineId, string flightNumber, CancellationToken cancellationToken);

    Task<bool> HasAnyFlightSchedulesAsync(int flightId, CancellationToken cancellationToken);

    Task AddAndSaveAsync(Flight flight, CancellationToken cancellationToken);
}
