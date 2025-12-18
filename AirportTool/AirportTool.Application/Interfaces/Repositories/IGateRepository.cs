using AirportTool.Domain;

namespace AirportTool.Application;

public interface IGateRepository : IRepository<Gate, int>
{
    Task<GetGateDto?> GetDtoByIdAsync(int id, CancellationToken cancellationToken);

    Task AddAndSaveAsync(Gate gate, CancellationToken cancellationToken);
}
