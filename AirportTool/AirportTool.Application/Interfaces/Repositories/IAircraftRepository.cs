using AirportTool.Domain;

namespace AirportTool.Application;

public interface IAircraftRepository : IRepository<Aircraft, int>
{
    Task<Aircraft?> GetByTailNumberAsync(string tailNumber, CancellationToken cancellationToken);

    Task<GetAircraftDto?> GetDtoByIdAsync(int id, CancellationToken cancellationToken);

    Task<PagedResult<GetAircraftDto>> GetDtoByFilterAsync(AircraftFilterDto filterDto, CancellationToken cancellationToken);

    Task AddAndSaveAsync(Aircraft aircraft, CancellationToken cancellationToken);
}
