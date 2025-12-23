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
        if (result.IsFailure || flight == null)
        {
            return result;
        }

        Gate? gate = null;
        if (dto.GateCode != null)
        {
            gate = await ValidateGateExistsAsync(dto.GateCode!, flight.OriginAirportId, result, cancellationToken);
        }

        Aircraft? assignedAircraft = null;
        if (dto.AssignedAircraftTail != null)
        {
            assignedAircraft = await ValidateAircraftExistsAsync(dto.AssignedAircraftTail, result, cancellationToken);
        }

        if (gate != null)
        {
            upsertResultDto.ScheduleConflicts = await ValidateGateOverlapsAsync(
                gateId: gate.GateId,
                gateCode: gate.GateCode,
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
        flightSchedule.Status = FlightScheduleStatus.Planned;
        flightSchedule.GateId = gate?.GateId;
        flightSchedule.AssignedAircraftId = assignedAircraft?.AircraftId;

        await _unitOfWork.FlightSchedules.AddAndSaveAsync(flightSchedule, cancellationToken);
        var getFlightScheduleDto = await ValidateFlightScheduleExistsAfterCreationAsync(flightSchedule.FlightScheduleId, result, cancellationToken);
        if (result.IsFailure || getFlightScheduleDto == null)
        {
            return result;
        }

        upsertResultDto.FlightSchedule = getFlightScheduleDto;

        return result;
    }

    public async Task<ImportSummaryDto> ImportAsync(IEnumerable<UpsertFlightScheduleDto> dtos, CancellationToken cancellationToken)
    {
        var rows = dtos.ToList();

        var summary = new ImportSummaryDto
        {
            Total = rows.Count
        };

        for (int i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var dto = rows[i];

            var existing = await _unitOfWork.FlightSchedules.GetByFlightAndDepartureAsync(dto.FlightId, dto.ScheduledDepartureUtc, cancellationToken);

            if (existing == null)
            {
                var createResult = await CreateAsync(dto, cancellationToken);

                if (createResult.IsFailure || (createResult.Value?.ScheduleConflicts?.Any() ?? false))
                {
                    summary.Failed++;
                    summary.Errors.Add(new ImportRowErrorDto
                    {
                        Row = rowNumber,
                        Message = BuildCreateRowErrorMessage(dto, createResult)
                    });
                    continue;
                }

                summary.Created++;
            }
            else
            {
                var updateResult = await UpdateExistingAsync(existing, dto, cancellationToken);

                if (updateResult.IsFailure)
                {
                    summary.Failed++;
                    summary.Errors.Add(new ImportRowErrorDto
                    {
                        Row = rowNumber,
                        Message = string.Join("; ", updateResult.Errors.Select(e => e.Message))
                    });
                    continue;
                }

                summary.Updated++;
            }
        }

        return summary;
    }

    private async Task<Result> UpdateExistingAsync(FlightSchedule existing, UpsertFlightScheduleDto dto, CancellationToken cancellationToken)
    {
        var result = new Result();

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            return result;
        }

        var flight = await ValidateFlightExistsAsync(dto.FlightId, result, cancellationToken);
        if (result.IsFailure || flight is null)
        {
            return result;
        }

        Gate? gate = null;
        if (dto.GateCode != null)
        {
            gate = await ValidateGateExistsAsync(dto.GateCode, flight.OriginAirportId, result, cancellationToken);
            if (result.IsFailure)
            {
                return result;
            }
        }

        Aircraft? aircraft = null;
        if (dto.AssignedAircraftTail != null)
        {
            aircraft = await ValidateAircraftExistsAsync(dto.AssignedAircraftTail, result, cancellationToken);
            if (result.IsFailure)
            {
                return result;
            }
        }

        if (gate != null)
        {
            await ValidateGateOverlapsAsync(
                gateId: gate.GateId,
                gateCode: gate.GateCode,
                proposedStartUtc: dto.ScheduledDepartureUtc,
                proposedEndUtc: dto.ScheduledArrivalUtc,
                excludeFlightScheduleId: existing.FlightScheduleId,
                result,
                cancellationToken);

            if (result.IsFailure)
            {
                return result;
            }
        }

        existing.ScheduledArrivalUtc = dto.ScheduledArrivalUtc;
        existing.Status = dto.Status;
        existing.GateId = gate?.GateId;
        existing.AssignedAircraftId = aircraft?.AircraftId;

        await _unitOfWork.FlightSchedules.UpdateAsync(existing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    #region Helper methods 

    private static string BuildCreateRowErrorMessage(UpsertFlightScheduleDto dto, Result<UpsertFlightScheduleResultDto> res)
    {
        if (res.Value?.ScheduleConflicts?.Any() == true)
        {
            var any = res.Value.ScheduleConflicts.First();

            return $"Gate overlap at {any.AirportIata}:{dto.GateCode} {FormatUtc(dto.ScheduledDepartureUtc)}–{FormatUtc(dto.ScheduledArrivalUtc)}";
        }

        if (res.Errors.Count > 0)
        {
            return string.Join("; ", res.Errors.Select(e => e.Message));
        }

        return "Unknown error";
    }

    private static string FormatUtc(DateTime utc)
    {
        return utc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'");
    }

    #endregion

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
        string gateCode,
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
                Message = $"Gate overlap at gate with code {gateCode}. {conflicts.Count} schedule conflict(s) found in the selected time window.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return conflicts;
    }

    private async Task<GetFlightScheduleDto?> ValidateFlightScheduleExistsAfterCreationAsync(
        int flightScheduleId, Result result, CancellationToken cancellationToken)
    {
        var flightScheduleDto = await _unitOfWork.FlightSchedules.GetDtoByIdAsync(flightScheduleId, cancellationToken);
        if (flightScheduleDto is null)
        {
            var error = new Error
            {
                Message = $"FlightSchedule with ID {flightScheduleId} does not exist.",
                Type = ErrorType.Unexpected
            };
            result.AddError(error);
        }
        return flightScheduleDto;
    }

    #endregion


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
