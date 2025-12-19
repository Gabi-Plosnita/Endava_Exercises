using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class FlightSchedulesService : IFlightSchedulesService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<BaseFilterDto> _baseFilterDtoValidator;
    private readonly IValidator<FlightScheduleFilterDto> _flightScheduleFilterDtoValidator;
    private ILogger<FlightSchedulesService> _logger;
    
    public FlightSchedulesService(
        IUnitOfWork unitOfWork,
        IValidator<BaseFilterDto> baseFilterDtoValidator,
        IValidator<FlightScheduleFilterDto> flightScheduleFilterDtoValidator,
        ILogger<FlightSchedulesService> logger)
    {
        _unitOfWork = unitOfWork;
        _baseFilterDtoValidator = baseFilterDtoValidator;
        _flightScheduleFilterDtoValidator = flightScheduleFilterDtoValidator;
        _logger = logger;
    }

    public async Task<GetFlightScheduleDto?> GetFlightScheduleDtoByIdAsync(int id, CancellationToken cancellationToken)
    {
        var getFlightScheduleDto = await _unitOfWork.FlightSchedules.GetDtoByIdAsync(id, cancellationToken);
        return getFlightScheduleDto;
    }

    public async Task<Result<PagedResult<FlightScheduleSearchDto>>> GetByFilterAsync(FlightScheduleFilterDto dto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<IReadOnlyList<DailyFlightStatsDto>>> GetDailyStatsAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<GetFlightScheduleDto?>> CreateAsync(UpsertFlightScheduleDto dto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<ImportResultDto> ImportAsync(IEnumerable<UpsertFlightScheduleDto> dtos, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
