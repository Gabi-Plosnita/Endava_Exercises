namespace AirportTool.Application;

public interface IFlightSchedulesService
{
    Task<GetFlightScheduleDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<PagedResult<FlightScheduleSearchDto>>> GetByFilterAsync(FlightScheduleFilterDto dto, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<DailyFlightStatsDto>>> GetDailyStatsAsync(DateOnly startUtc, DateOnly endUtc, CancellationToken cancellationToken);

    Task<Result<UpsertFlightScheduleResultDto>> CreateAsync(UpsertFlightScheduleDto dto, CancellationToken cancellationToken);

    Task<ImportResultDto> ImportAsync(IEnumerable<UpsertFlightScheduleDto> dtos, CancellationToken cancellationToken);
}
