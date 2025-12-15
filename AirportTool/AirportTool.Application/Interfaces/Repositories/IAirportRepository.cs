using AirportTool.Domain;

namespace AirportTool.Application;

public interface IAirportRepository : IRepository<Airport, int>
{
    Task<Airport?> GetByIataCodeAsync(string iataCode, CancellationToken cancellationToken = default);
}
