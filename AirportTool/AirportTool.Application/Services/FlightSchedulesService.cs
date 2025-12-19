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
        var result = new Result<PagedResult<FlightScheduleSearchDto>>();

        var dtoValidationResult = _baseFilterDtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            return result;
        }

        var filteredDtoResult = await _unitOfWork.FlightSchedules.GetFilteredFlightSchedulesAsync(dto, cancellationToken);
        result.Value = filteredDtoResult;

        return result;
    }

    public async Task<Result<IReadOnlyList<DailyFlightStatsDto>>> GetDailyStatsAsync(
        DateOnly startUtc, DateOnly endUtc, CancellationToken cancellationToken)
    {
        var result = new Result<IReadOnlyList<DailyFlightStatsDto>>();

        ValidateStartAndEndUtc(startUtc, endUtc, result);
        if (result.IsFailure)
        {
            return result;
        }

        var statsDto = await _unitOfWork.FlightSchedules.GetDailyStatsAsync(startUtc, endUtc, cancellationToken);
        result.Value = statsDto;
        return result;
    }

    public async Task<Result<GetFlightScheduleDto?>> CreateAsync(UpsertFlightScheduleDto dto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<ImportResultDto> ImportAsync(IEnumerable<UpsertFlightScheduleDto> dtos, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    #region Business Rules Methods

    private void ValidateStartAndEndUtc(DateOnly startUtc, DateOnly endUtc, Result result)
    {
        if (startUtc >= endUtc)
        {
            var error = new Error
            {
                Message = "The startUtc must be earlier than endUtc.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }

    }

    #endregion
}
