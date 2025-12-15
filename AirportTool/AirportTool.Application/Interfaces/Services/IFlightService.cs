namespace AirportTool.Application;

public interface IFlightService
{
    Task<Result<GetFlightDto?>> GetByIdAsync(int flightId, CancellationToken cancellationToken);

    Task<Result<GetFlightDto?>> CreateAsync(CreateFlightDto dto, CancellationToken cancellationToken);

    Task<Result> UpdateAsync(int flightId, CreateFlightDto dto, CancellationToken cancellationToken);

    Task<Result> DeleteByIdAsync(int flightId, CancellationToken cancellationToken);
}
