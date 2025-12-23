using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class FlightSchedulesService : IFlightSchedulesService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDtoValidator _dtoValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<FlightSchedulesService> _logger;

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

    public async Task<Result<PagedResult<FlightScheduleSearchDto>>> GetByFilterAsync(FlightScheduleFilterDto dto,CancellationToken cancellationToken)
    {
        var result = new Result<PagedResult<FlightScheduleSearchDto>>();
        LogGetByFilterStart(dto);

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            LogGetByFilterFailure(dto, result);
            return result;
        }

        var filteredDtoResult = await _unitOfWork.FlightSchedules.GetFilteredFlightSchedulesAsync(dto, cancellationToken);
        result.Value = filteredDtoResult;

        LogGetByFilterSuccess(dto, filteredDtoResult);
        return result;
    }

    public async Task<Result<IReadOnlyList<DailyFlightStatsDto>>> GetDailyStatsAsync(
        DateOnly startUtc, DateOnly endUtc,CancellationToken cancellationToken)
    {
        var result = new Result<IReadOnlyList<DailyFlightStatsDto>>();
        LogGetDailyStatsStart(startUtc, endUtc);

        ValidateStartAndEndUtc(startUtc, endUtc, result);
        if (result.IsFailure)
        {
            LogGetDailyStatsFailure(startUtc, endUtc, result);
            return result;
        }

        var statsDto = await _unitOfWork.FlightSchedules.GetDailyStatsAsync(startUtc, endUtc, cancellationToken);
        result.Value = statsDto;

        LogGetDailyStatsSuccess(startUtc, endUtc, statsDto);
        return result;
    }

    public async Task<Result<UpsertFlightScheduleResultDto>> CreateAsync(UpsertFlightScheduleDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<UpsertFlightScheduleResultDto>();
        var upsertResultDto = new UpsertFlightScheduleResultDto();
        result.Value = upsertResultDto;

        LogCreateStart(dto);

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);

        if (result.IsFailure)
        {
            LogCreateFailure(dto, result);
            return result;
        }

        var flight = await ValidateFlightExistsAsync(dto.FlightId, result, cancellationToken);
        if (result.IsFailure || flight == null)
        {
            LogCreateFailure(dto, result);
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
                gateCode: gate.Code,
                proposedStartUtc: dto.ScheduledDepartureUtc,
                proposedEndUtc: dto.ScheduledArrivalUtc,
                excludeFlightScheduleId: null,
                result,
                cancellationToken);
        }

        if (result.IsFailure || upsertResultDto.ScheduleConflicts.Any())
        {
            LogCreateFailure(dto, result);
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
            LogFlightScheduleNotFoundAfterCreation(flightSchedule.FlightScheduleId);
            LogCreateFailure(dto, result);
            return result;
        }

        upsertResultDto.FlightSchedule = getFlightScheduleDto;

        LogCreateSuccess(flightSchedule);
        return result;
    }

    public async Task<ImportSummaryDto> ImportAsync(IEnumerable<UpsertFlightScheduleDto> dtos, CancellationToken cancellationToken)
    {
        var rows = dtos.ToList();

        var summary = new ImportSummaryDto
        {
            Total = rows.Count
        };

        LogImportStart(summary.Total);

        for (int i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var dto = rows[i];

            LogImportRowStart(rowNumber, dto);

            var existing = await _unitOfWork.FlightSchedules.GetByFlightAndDepartureAsync(dto.FlightId, dto.ScheduledDepartureUtc, cancellationToken);

            if (existing == null)
            {
                var createResult = await CreateAsync(dto, cancellationToken);

                if (createResult.IsFailure || (createResult.Value?.ScheduleConflicts?.Any() ?? false))
                {
                    summary.Failed++;
                    var message = BuildCreateRowErrorMessage(dto, createResult);

                    summary.Errors.Add(new ImportRowErrorDto
                    {
                        Row = rowNumber,
                        Message = message
                    });

                    LogImportRowFailure(rowNumber, dto, message, createResult.Errors);
                    continue;
                }

                summary.Created++;
                LogImportRowCreated(rowNumber, dto);
            }
            else
            {
                var updateResult = await UpdateExistingAsync(existing, dto, cancellationToken);

                if (updateResult.IsFailure)
                {
                    summary.Failed++;
                    var msg = string.Join("; ", updateResult.Errors.Select(e => e.Message));

                    summary.Errors.Add(new ImportRowErrorDto
                    {
                        Row = rowNumber,
                        Message = msg
                    });

                    LogImportRowFailure(rowNumber, dto, msg, updateResult.Errors);
                    continue;
                }

                summary.Updated++;
                LogImportRowUpdated(rowNumber, dto, existing.FlightScheduleId);
            }
        }

        LogImportEnd(summary);
        return summary;
    }

    private async Task<Result> UpdateExistingAsync(FlightSchedule existing, UpsertFlightScheduleDto dto, CancellationToken cancellationToken)
    {
        var result = new Result();
        LogUpdateStart(existing.FlightScheduleId, dto);

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            LogUpdateFailure(existing.FlightScheduleId, result);
            return result;
        }

        var flight = await ValidateFlightExistsAsync(dto.FlightId, result, cancellationToken);
        if (result.IsFailure || flight is null)
        {
            LogUpdateFailure(existing.FlightScheduleId, result);
            return result;
        }

        Gate? gate = null;
        if (dto.GateCode != null)
        {
            gate = await ValidateGateExistsAsync(dto.GateCode, flight.OriginAirportId, result, cancellationToken);
            if (result.IsFailure)
            {
                LogUpdateFailure(existing.FlightScheduleId, result);
                return result;
            }
        }

        Aircraft? aircraft = null;
        if (dto.AssignedAircraftTail != null)
        {
            aircraft = await ValidateAircraftExistsAsync(dto.AssignedAircraftTail, result, cancellationToken);
            if (result.IsFailure)
            {
                LogUpdateFailure(existing.FlightScheduleId, result);
                return result;
            }
        }

        if (gate != null)
        {
            await ValidateGateOverlapsAsync(
                gateId: gate.GateId,
                gateCode: gate.Code,
                proposedStartUtc: dto.ScheduledDepartureUtc,
                proposedEndUtc: dto.ScheduledArrivalUtc,
                excludeFlightScheduleId: existing.FlightScheduleId,
                result,
                cancellationToken);

            if (result.IsFailure)
            {
                LogUpdateFailure(existing.FlightScheduleId, result);
                return result;
            }
        }

        existing.ScheduledArrivalUtc = dto.ScheduledArrivalUtc;
        existing.Status = dto.Status;
        existing.GateId = gate?.GateId;
        existing.AssignedAircraftId = aircraft?.AircraftId;

        await _unitOfWork.FlightSchedules.UpdateAsync(existing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogUpdateSuccess(existing);
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
                Message = $"Gate with Code {gateCode} not found for airport {airportId}",
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
        int flightScheduleId,
        Result result,
        CancellationToken cancellationToken)
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
            _logger.LogDebug("Retrieved FlightSchedule with ID {FlightScheduleId}.", id);
        }
        else
        {
            _logger.LogDebug("FlightSchedule with ID {FlightScheduleId} not found.", id);
        }
    }

    private void LogGetByFilterStart(FlightScheduleFilterDto dto)
    {
        _logger.LogInformation(
            @"Filtering flight schedules:
                OriginIata={OriginIata},
                DestinationIata={DestinationIata},
                Date={Date}",
            dto.OriginIata,
            dto.DestinationIata,
            dto.Date);
    }

    private void LogGetByFilterFailure(FlightScheduleFilterDto dto, Result<PagedResult<FlightScheduleSearchDto>> result)
    {
        _logger.LogWarning(
            @"Filter flight schedules failed:
                Errors={Errors}",
            result.Errors);
    }

    private void LogGetByFilterSuccess(FlightScheduleFilterDto dto, PagedResult<FlightScheduleSearchDto> paged)
    {
        _logger.LogInformation(
            @"Filtered flight schedules successfully:
                PageIndex={PageIndex},
                PageSize={PageSize},
                TotalCount={TotalCount}",
            paged.PageIndex,
            paged.PageSize,
            paged.TotalCount);
    }

    private void LogGetDailyStatsStart(DateOnly startUtc, DateOnly endUtc)
    {
        _logger.LogInformation(
            @"Getting daily flight stats:
                StartUtc={StartUtc},
                EndUtc={EndUtc}",
            startUtc,
            endUtc);
    }

    private void LogGetDailyStatsFailure(DateOnly startUtc, DateOnly endUtc, Result result)
    {
        _logger.LogWarning(
            @"Get daily flight stats failed:
                StartUtc={StartUtc},
                EndUtc={EndUtc},
                Errors={Errors}",
            startUtc,
            endUtc,
            result.Errors);
    }

    private void LogGetDailyStatsSuccess(DateOnly startUtc, DateOnly endUtc, IReadOnlyList<DailyFlightStatsDto> stats)
    {
        _logger.LogInformation(
            @"Daily flight stats retrieved successfully:
                StartUtc={StartUtc},
                EndUtc={EndUtc},
                Days={Days}",
            startUtc,
            endUtc,
            stats.Count);
    }

    private void LogCreateStart(UpsertFlightScheduleDto dto)
    {
        _logger.LogInformation(
            @"Creating flight schedule:
                FlightId={FlightId},
                ScheduledDepartureUtc={ScheduledDepartureUtc},
                ScheduledArrivalUtc={ScheduledArrivalUtc},
                GateCode={GateCode},
                AssignedAircraftTail={AssignedAircraftTail},
                Status={Status}",
            dto.FlightId,
            dto.ScheduledDepartureUtc,
            dto.ScheduledArrivalUtc,
            dto.GateCode,
            dto.AssignedAircraftTail,
            dto.Status);
    }

    private void LogCreateFailure(UpsertFlightScheduleDto dto, Result<UpsertFlightScheduleResultDto> result)
    {
        _logger.LogWarning(
            @"Create flight schedule failed:
                FlightId={FlightId},
                ScheduledDepartureUtc={ScheduledDepartureUtc},
                GateCode={GateCode},
                Errors={Errors}",
            dto.FlightId,
            dto.ScheduledDepartureUtc,
            dto.GateCode,
            result.Errors);
    }

    private void LogCreateSuccess(FlightSchedule schedule)
    {
        _logger.LogInformation(
            @"Flight schedule created successfully:
                FlightScheduleId={FlightScheduleId},
                FlightId={FlightId},
                GateId={GateId},
                AssignedAircraftId={AssignedAircraftId},
                ScheduledDepartureUtc={ScheduledDepartureUtc},
                ScheduledArrivalUtc={ScheduledArrivalUtc},
                Status={Status}",
            schedule.FlightScheduleId,
            schedule.FlightId,
            schedule.GateId,
            schedule.AssignedAircraftId,
            schedule.ScheduledDepartureUtc,
            schedule.ScheduledArrivalUtc,
            schedule.Status);
    }

    private void LogUpdateStart(int flightScheduleId, UpsertFlightScheduleDto dto)
    {
        _logger.LogInformation(
            @"Updating flight schedule:
                FlightScheduleId={FlightScheduleId},
                FlightId={FlightId},
                ScheduledDepartureUtc={ScheduledDepartureUtc},
                ScheduledArrivalUtc={ScheduledArrivalUtc},
                GateCode={GateCode},
                AssignedAircraftTail={AssignedAircraftTail},
                Status={Status}",
            flightScheduleId,
            dto.FlightId,
            dto.ScheduledDepartureUtc,
            dto.ScheduledArrivalUtc,
            dto.GateCode,
            dto.AssignedAircraftTail,
            dto.Status);
    }

    private void LogUpdateFailure(int flightScheduleId, Result result)
    {
        _logger.LogWarning(
            @"Update flight schedule failed:
                FlightScheduleId={FlightScheduleId},
                Errors={Errors}",
            flightScheduleId,
            result.Errors);
    }

    private void LogUpdateSuccess(FlightSchedule schedule)
    {
        _logger.LogInformation(
            @"Flight schedule updated successfully:
                FlightScheduleId={FlightScheduleId},
                FlightId={FlightId},
                Status={Status}",
            schedule.FlightScheduleId,
            schedule.FlightId,
            schedule.Status);
    }

    private void LogImportStart(int totalRows)
    {
        _logger.LogInformation(
            @"Importing flight schedules:
                TotalRows={TotalRows}",
            totalRows);
    }

    private void LogImportRowStart(int rowNumber, UpsertFlightScheduleDto dto)
    {
        _logger.LogDebug(
            @"Import row:
                Row={Row},
                FlightId={FlightId},
                ScheduledDepartureUtc={ScheduledDepartureUtc},
                ScheduledArrivalUtc={ScheduledArrivalUtc},
                GateCode={GateCode},
                AssignedAircraftTail={AssignedAircraftTail},
                Status={Status}",
            rowNumber,
            dto.FlightId,
            dto.ScheduledDepartureUtc,
            dto.ScheduledArrivalUtc,
            dto.GateCode,
            dto.AssignedAircraftTail,
            dto.Status);
    }

    private void LogImportRowCreated(int rowNumber, UpsertFlightScheduleDto dto)
    {
        _logger.LogInformation(
            @"Import row created:
                Row={Row},
                FlightId={FlightId},
                ScheduledDepartureUtc={ScheduledDepartureUtc}",
            rowNumber,
            dto.FlightId,
            dto.ScheduledDepartureUtc);
    }

    private void LogImportRowUpdated(int rowNumber, UpsertFlightScheduleDto dto, int flightScheduleId)
    {
        _logger.LogInformation(
            @"Import row updated:
                Row={Row},
                FlightScheduleId={FlightScheduleId},
                FlightId={FlightId},
                ScheduledDepartureUtc={ScheduledDepartureUtc}",
            rowNumber,
            flightScheduleId,
            dto.FlightId,
            dto.ScheduledDepartureUtc);
    }

    private void LogImportRowFailure(int rowNumber, UpsertFlightScheduleDto dto, string message, IReadOnlyList<Error> errors)
    {
        _logger.LogWarning(
            @"Import row failed:
                Row={Row},
                FlightId={FlightId},
                ScheduledDepartureUtc={ScheduledDepartureUtc},
                GateCode={GateCode},
                Message={Message},
                Errors={Errors}",
            rowNumber,
            dto.FlightId,
            dto.ScheduledDepartureUtc,
            dto.GateCode,
            message,
            errors);
    }

    private void LogImportEnd(ImportSummaryDto summary)
    {
        _logger.LogInformation(
            @"Import finished:
                Total={Total},
                Created={Created},
                Updated={Updated},
                Failed={Failed}",
            summary.Total,
            summary.Created,
            summary.Updated,
            summary.Failed);
    }

    private void LogFlightScheduleNotFoundAfterCreation(int flightScheduleId)
    {
        _logger.LogError(
            @"FlightSchedule with ID {FlightScheduleId} not found after creation.",
            flightScheduleId);
    }

    #endregion
}
