using AirportTool.Domain;

namespace AirportTool.Application;

public interface IAirlineRepository : IRepository<Airline, int>
{
    Task<Airline?> GetByIataCodeAsync(string iataCode, CancellationToken cancellationToken = default);
}
