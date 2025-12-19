using AirportTool.Domain;

namespace AirportTool.Application;

public interface IGateRepository : IRepository<Gate, int>
{
    Task<GetGateDto?> GetDtoByIdAsync(int id, CancellationToken cancellationToken);

    Task<Gate?> GetByAirlineIdAndCodeAsync(int airportId, string code, CancellationToken cancellationToken);

    Task<Gate?> GetByCodeAndAirportAsync(string code, int airportId, CancellationToken cancellationToken);

    Task AddAndSaveAsync(Gate gate, CancellationToken cancellationToken);
}
