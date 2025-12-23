using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class FlightSchedulesService : IFlightSchedulesService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDtoValidator _dtoValidator;
    private readonly IMapper _mapper;
    private ILogger<FlightSchedulesService> _logger;
    
    public FlightSchedulesService(
        IUnitOfWork unitOfWork,
        IDtoValidator dtoValidator,
        IMapper mapper,
        ILogger<FlightSchedulesService> logger)
    {
        _unitOfWork = unitOfWork;
        _dtoValidator = dtoValidator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<GetFlightScheduleDto?>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = new Result<GetFlightScheduleDto?>();
        var getFlightScheduleDto = await _unitOfWork.FlightSchedules.GetDtoByIdAsync(id, cancellationToken);
        var found = getFlightScheduleDto != null;

        if (!found)
        {
            result.AddError(new Error
            {
                Message = $"FlightSchedule with ID {id} not found.",
                Type = ErrorType.NotFound
            });
        }

        LogGetById(id, found);
        result.Value = getFlightScheduleDto;
        return result;
    }

    public async Task<Result<PagedResult<FlightScheduleSearchDto>>> GetByFilterAsync(FlightScheduleFilterDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<PagedResult<FlightScheduleSearchDto>>();

        var dtoValidationResult = _dtoValidator.Validate(dto);
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

    public async Task<Result<UpsertFlightScheduleResultDto>> CreateAsync(UpsertFlightScheduleDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<UpsertFlightScheduleResultDto>();
        var upsertResultDto = new UpsertFlightScheduleResultDto();
        result.Value = upsertResultDto;

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);

        if (result.IsFailure)
        {
            return result;
        }

        var flight = await ValidateFlightExistsAsync(dto.FlightId, result, cancellationToken);
        if(result.IsFailure || flight == null)
        {
            return result;
        }

        Gate? gate = null;
        if(dto.GateCode != null)
        {
            gate = await ValidateGateExistsAsync(dto.GateCode!, flight.OriginAirportId , result, cancellationToken);
        }

        Aircraft? assignedAircraft = null;
        if(dto.AssignedAircraftTail != null)
        {
            assignedAircraft = await ValidateAircraftExistsAsync(dto.AssignedAircraftTail, result, cancellationToken);
        }

        if(gate != null)
        {
            upsertResultDto.ScheduleConflicts = await ValidateGateOverlapsAsync(
                gateId: gate.GateId,
                proposedStartUtc: dto.ScheduledDepartureUtc,
                proposedEndUtc: dto.ScheduledArrivalUtc,
                excludeFlightScheduleId: null,
                result,
                cancellationToken);
        }

        if (result.IsFailure || upsertResultDto.ScheduleConflicts.Any())
        {
            return result;
        }

        var flightSchedule = _mapper.Map<FlightSchedule>(dto);
        flightSchedule.GateId = gate?.GateId;
        flightSchedule.AssignedAircraftId = assignedAircraft?.AircraftId;

        await _unitOfWork.FlightSchedules.AddAndSaveAsync(flightSchedule, cancellationToken);
        var getFlightScheduleDto = await _unitOfWork.FlightSchedules.GetDtoByIdAsync(flightSchedule.FlightScheduleId, cancellationToken);
        upsertResultDto.FlightSchedule = getFlightScheduleDto;

        return result;  
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

    private async Task<Flight?> ValidateFlightExistsAsync(int flightId, Result result, CancellationToken cancellationToken)
    {
        var flight = await _unitOfWork.Flights.GetByIdAsync(flightId, cancellationToken);
        if (flight is null)
        {
            var error = new Error
            {
                Message = $"Flight with ID {flightId} does not exist.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return flight;
    }

    private async Task<Gate?> ValidateGateExistsAsync(string gateCode, int airportId, Result result, CancellationToken cancellationToken)
    {
        var gate = await _unitOfWork.Gates.GetByCodeAndAirportAsync(gateCode, airportId, cancellationToken);
        if (gate is null)
        {
            var error = new Error
            {
                Message = $"Gate with Code {gateCode} not found",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return gate;
    }

    private async Task<Aircraft?> ValidateAircraftExistsAsync(string assignedAircraftTail, Result result, CancellationToken cancellationToken)
    {
        var aircraft = await _unitOfWork.Aircrafts.GetByTailNumberAsync(assignedAircraftTail, cancellationToken);
        if (aircraft is null)
        {
            var error = new Error
            {
                Message = $"Aircraft with Tail {assignedAircraftTail} not found",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return aircraft;
    }

    private async Task<IReadOnlyList<ScheduleConflictDto>> ValidateGateOverlapsAsync(
        int gateId,
        DateTime proposedStartUtc,
        DateTime proposedEndUtc,
        int? excludeFlightScheduleId,
        Result result,
        CancellationToken cancellationToken)
    {
        var conflicts = await _unitOfWork.FlightSchedules.GetGateOverlapsAsync(
            gateId: gateId,
            proposedStartUtc: proposedStartUtc,
            proposedEndUtc: proposedEndUtc,
            excludeFlightScheduleId: excludeFlightScheduleId,
            cancellationToken);
        if (conflicts.Any())
        {
            var error = new Error
            {
                Message = $"Gate has {conflicts.Count} schedule conflict(s) in the selected time window.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return conflicts;
    }

    #endregion

    //TODO: Logging//

    #region Logging Methods

    private void LogGetById(int id, bool found)
    {
        if (found)
        {
            _logger.LogDebug("FlightSchedule with ID {FlightScheduleId} retrieved successfully.", id);
        }
        else
        {
            _logger.LogDebug("FlightSchedule with ID {FlightScheduleId} not found.", id);
        }
    }

    #endregion
}
