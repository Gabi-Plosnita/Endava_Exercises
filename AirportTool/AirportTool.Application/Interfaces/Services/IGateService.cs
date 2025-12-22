namespace AirportTool.Application;

public interface IGateService
{
    Task<Result<GetGateDto?>> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<GetGateDto?>> CreateAsync(CreateGateDto dto, CancellationToken cancellationToken);

    Task<Result> UpdateAsync(int id, UpdateGateDto dto, CancellationToken cancellationToken);

    Task<Result> DeleteByIdAsync(int id, CancellationToken cancellationToken);
}
