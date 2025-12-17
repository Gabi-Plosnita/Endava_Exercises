namespace AirportTool.Application;

public interface IAircraftService
{
    Task<GetAircraftDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<PagedResult<GetAircraftDto>>> GetByFilterAsync(AircraftFilterDto dto, CancellationToken cancellationToken);

    Task<Result<GetAircraftDto?>> CreateAsync(CreateAircraftDto dto, CancellationToken cancellationToken);

    Task<Result> UpdateAsync(int id, UpdateAircraftDto dto, CancellationToken cancellationToken);

    Task<Result> DeleteByIdAsync(int id, CancellationToken cancellationToken);
}
