using AirportTool.Domain;

namespace AirportTool.Application;

public interface IAircraftRepository : IRepository<Aircraft, int>
{
    Task<Aircraft?> GetByTailNumberAsync(string tailNumber, CancellationToken cancellationToken);

    Task<GetAircraftDto?> GetDtoByIdAsync(int id, CancellationToken cancellationToken);
}
