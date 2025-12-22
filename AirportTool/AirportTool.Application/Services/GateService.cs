using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class GateService : IGateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDtoValidator _dtoValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<GateService> _logger;

    public GateService(IUnitOfWork unitOfWork,
                       IDtoValidator dtoValidator,
                       IMapper mapper,
                       ILogger<GateService> logger)
    {
        _unitOfWork = unitOfWork;
        _dtoValidator = dtoValidator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<GetGateDto?>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = new Result<GetGateDto?>(); 

        var getGateDto = await _unitOfWork.Gates.GetDtoByIdAsync(id, cancellationToken);
        var found = getGateDto != null;

        if(!found)
        {
            result.AddError(new Error
            {
                Message = $"Gate with Id {id} not found.",
                Type = ErrorType.NotFound
            });
        }

        LogGetById(id, found);
        result.Value = getGateDto;
        return result;
    }

    public async Task<Result<GetGateDto?>> CreateAsync(CreateGateDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<GetGateDto?>();

        LogCreateStart(dto);

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            LogCreateFailure(dto, result);
            return result;
        }

        var airport = await ValidateAirportExistsAsync(dto.AirportIataCode, result, cancellationToken);
        if (airport != null)
        {
            await ValidateGateCodeIsUniqueForAirport(
                gateToUpdateId: null,
                airportId: airport.AirportId,
                code: dto.Code,
                result,
                cancellationToken);
        }

        if (result.IsFailure)
        {
            LogCreateFailure(dto, result);
            return result;
        }

        var gate = _mapper.Map<Gate>(dto);
        gate.AirportId = airport!.AirportId;

        await _unitOfWork.Gates.AddAndSaveAsync(gate, cancellationToken);
        var getGateDto = await _unitOfWork.Gates.GetDtoByIdAsync(gate.GateId, cancellationToken);
        if(getGateDto == null)
        {
            result.AddError(new Error
            {
                Message = $"Gate with Id {gate.GateId} not found after creation.",
                Type = ErrorType.Unexpected
            });
            LogGateNotFoundAfterCreation(gate.GateId);
            LogCreateFailure(dto, result);
            return result;
        }

        result.Value = getGateDto;

        LogCreateSuccess(gate);
        return result;
    }

    public async Task<Result> UpdateAsync(int id, UpdateGateDto dto, CancellationToken cancellationToken)
    {
        var result = new Result();

        LogUpdateStart(id, dto);

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            LogUpdateFailure(id, result);
            return result;
        }

        var existingGate = await ValidateGateExistsAsync(id, result, cancellationToken);
        if (result.IsFailure || existingGate == null)
        {
            LogUpdateFailure(id, result);
            return result;
        }

        await ValidateGateCodeIsUniqueForAirport(
            gateToUpdateId: existingGate.GateId,
            airportId: existingGate.AirportId,
            code: dto.GateCode,
            result,
            cancellationToken);

        if (result.IsFailure)
        {
            LogUpdateFailure(id, result);
            return result;
        }

        _mapper.Map(dto, existingGate);

        await _unitOfWork.Gates.UpdateAsync(existingGate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogUpdateSuccess(existingGate);
        return result;
    }

    public async Task<Result> DeleteByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = new Result();

        LogDeleteStart(id);

        var gate = await ValidateGateExistsAsync(id, result, cancellationToken);
        if (result.IsFailure || gate == null)
        {
            LogDeleteFailure(id, result);
            return result;
        }

        await _unitOfWork.Gates.RemoveAsync(gate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogDeleteSuccess(id);
        return result;
    }

    #region Business Rules Methods

    private async Task<Airport?> ValidateAirportExistsAsync(string airportIataCode, Result result, CancellationToken cancellationToken)
    {
        var airport = await _unitOfWork.Airports.GetByIataCodeAsync(airportIataCode, cancellationToken);
        if (airport == null)
        {
            var error = new Error
            {
                Message = $"Airport with Iata Code {airportIataCode} not found.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return airport;
    }

    private async Task<Gate?> ValidateGateExistsAsync(int id, Result result, CancellationToken cancellationToken)
    {
        var gate = await _unitOfWork.Gates.GetByIdAsync(id, cancellationToken);
        if (gate == null)
        {
            var error = new Error
            {
                Message = $"Gate with Id {id} not found.",
                Type = ErrorType.NotFound
            };
            result.AddError(error);
        }
        return gate;
    }

    private async Task ValidateGateCodeIsUniqueForAirport(
        int? gateToUpdateId, int airportId, string code, Result result, CancellationToken cancellationToken)
    {
        var existingGate = await _unitOfWork.Gates.GetByAirlineIdAndCodeAsync(airportId, code, cancellationToken);
        if (existingGate != null && existingGate.GateId != gateToUpdateId)
        {
            var error = new Error
            {
                Message = $"Gate with Code {code} already exists for Airport {airportId}.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }

    #endregion

    #region Logging Methods

    private void LogGetById(int gateId, bool found)
    {
        if (found)
        {
            _logger.LogDebug("Retrieved gate with ID {GateId}.", gateId);
        }
        else
        {
            _logger.LogDebug("Gate with ID {GateId} not found.", gateId);
        }
    }

    private void LogCreateStart(CreateGateDto dto)
    {
        _logger.LogInformation(
            @"Creating gate:
                AirportIataCode={AirportIataCode},
                Code={Code}",
            dto.AirportIataCode,
            dto.Code);
    }

    private void LogCreateFailure(CreateGateDto dto, Result result)
    {
        _logger.LogWarning(
            @"Create gate failed:
                AirportIataCode={AirportIataCode},
                Code={Code},
                Errors={Errors}",
            dto.AirportIataCode,
            dto.Code,
            result.Errors);
    }

    private void LogCreateSuccess(Gate gate)
    {
        _logger.LogInformation(
            @"Gate created successfully:
                GateId={GateId},
                AirportId={AirportId},
                Code={Code}",
            gate.GateId,
            gate.AirportId,
            gate.Code);
    }

    private void LogUpdateStart(int gateId, UpdateGateDto dto)
    {
        _logger.LogInformation(
            @"Updating gate:
                GateId={GateId},
                Code={Code}",
            gateId,
            dto.GateCode);
    }

    private void LogUpdateFailure(int gateId, Result result)
    {
        _logger.LogWarning(
            @"Update gate failed:
                GateId={GateId},
                Errors={Errors}",
            gateId,
            result.Errors);
    }

    private void LogUpdateSuccess(Gate gate)
    {
        _logger.LogInformation(
            @"Gate updated successfully:
                GateId={GateId},
                AirportId={AirportId},
                Code={Code}",
            gate.GateId,
            gate.AirportId,
            gate.Code);
    }

    private void LogDeleteStart(int gateId)
    {
        _logger.LogInformation(
            @"Deleting gate:
                GateId={GateId}",
            gateId);
    }

    private void LogDeleteFailure(int gateId, Result result)
    {
        _logger.LogWarning(
            @"Delete gate failed:
                GateId={GateId},
                Errors={Errors}",
            gateId,
            result.Errors);
    }

    private void LogDeleteSuccess(int gateId)
    {
        _logger.LogInformation(
            @"Gate deleted successfully:
                GateId={GateId}",
            gateId);
    }

    private void LogGateNotFoundAfterCreation(int gateId)
    {
        _logger.LogError(
            @"Gate with ID {GateId} not found after creation.",
            gateId);
    }

    #endregion
}
