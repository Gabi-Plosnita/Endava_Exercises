using AirportTool.Domain;

namespace AirportTool.Application;

public interface IFlightRepository : IRepository<Flight, int>
{
    Task<Flight?> GetFlightByAirlineAndFlightNumberAsync(
        string airlineIataCode, string flightNumber, CancellationToken cancellationToken = default);
}
