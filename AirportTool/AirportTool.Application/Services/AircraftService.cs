using AirportTool.Domain;
using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class AircraftService : IAircraftService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateAircraftDto> _createAircraftDtoValidator;
    private readonly IValidator<UpdateAircraftDto> _updateAircraftDtoValidator;
    private readonly ILogger<AircraftService> _logger;

    public AircraftService(IUnitOfWork unitOfWork,
                           IValidator<CreateAircraftDto> createAircraftDtoValidator,
                           IValidator<UpdateAircraftDto> updateAircraftDtoValidator,
                           ILogger<AircraftService> logger)
    {
        _unitOfWork = unitOfWork;
        _createAircraftDtoValidator = createAircraftDtoValidator;
        _updateAircraftDtoValidator = updateAircraftDtoValidator;
        _logger = logger;
    }

    public async Task<GetAircraftDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var getAircraftDto = await _unitOfWork.Aircrafts.GetDtoByIdAsync(id, cancellationToken);
        var found = getAircraftDto != null;
        LogGetById(id, found);
        return getAircraftDto;
    }

    public async Task<Result<PagedResult<GetAircraftDto>>> GetByFilterAsync(AircraftFilterDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<PagedResult<GetAircraftDto>>();
        var filteredResult = await _unitOfWork.Aircrafts.GetDtoByFilterAsync(dto, cancellationToken);
        result.Value = filteredResult;
        return result;
    }

    public async Task<Result<GetAircraftDto?>> CreateAsync(CreateAircraftDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<GetAircraftDto?>();

        var dtoValidationResult = _createAircraftDtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            return result;
        }

        await ValidateAircraftTailIsUniqueAsync(aircraftToUpdateId: null, dto.TailNumber, result, cancellationToken);

        Airline? airline = null;
        if (!string.IsNullOrEmpty(dto.OwnedByAirlineIataCode))
        {
            airline = await ValidateAirlineExistsAsync(dto.OwnedByAirlineIataCode, result, cancellationToken);
        }

        if (result.IsFailure)
        {
            return result;
        }

        var aircraft = new Aircraft
        {
            TailNumber = dto.TailNumber,
            Model = dto.Model,
            SeatCapacity = dto.SeatCapacity,
            OwnedByAirlineId = airline?.AirlineId
        };

        await _unitOfWork.Aircrafts.AddAndSaveAsync(aircraft, cancellationToken);
        var getAircraftDto = await _unitOfWork.Aircrafts.GetDtoByIdAsync(aircraft.AircraftId, cancellationToken);
        result.Value = getAircraftDto;

        return result;
    }

    public async Task<Result> UpdateAsync(int id, UpdateAircraftDto dto, CancellationToken cancellationToken)
    {
        var result = new Result();

        var dtoValidationResult = _updateAircraftDtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            return result;
        }

        var existingAircraft = await ValidateAircraftExistsAsync(id, result, cancellationToken);
        if (result.IsFailure || existingAircraft == null)
        {
            return result;
        }

        await ValidateAircraftTailIsUniqueAsync(aircraftToUpdateId: id, dto.TailNumber, result, cancellationToken);

        Airline? airline = null;
        if (!string.IsNullOrEmpty(dto.OwnedByAirlineIataCode))
        {
            airline = await ValidateAirlineExistsAsync(dto.OwnedByAirlineIataCode, result, cancellationToken);
        }

        if (result.IsFailure)
        {
            return result;
        }

        existingAircraft.TailNumber = dto.TailNumber;
        existingAircraft.Model = dto.Model;
        existingAircraft.SeatCapacity = dto.SeatCapacity;
        existingAircraft.OwnedByAirlineId = airline?.AirlineId;

        await _unitOfWork.Aircrafts.UpdateAsync(existingAircraft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result> DeleteByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = new Result();

        var existingAircraft = await ValidateAircraftExistsAsync(id, result, cancellationToken);
        if (result.IsFailure || existingAircraft == null)
        {
            return result;
        }

        await _unitOfWork.Aircrafts.RemoveAsync(existingAircraft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    #region Validation Methods
    private async Task ValidateAircraftTailIsUniqueAsync(
        int? aircraftToUpdateId, string tailNumber, Result result, CancellationToken cancellationToken)
    {
        var existingAircraft = await _unitOfWork.Aircrafts.GetByTailNumberAsync(tailNumber, cancellationToken);
        if (existingAircraft != null && existingAircraft.AircraftId != aircraftToUpdateId)
        {
            var error = new Error
            {
                Message = $"An aircraft with tail number '{tailNumber}' already exists.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }

    private async Task<Airline?> ValidateAirlineExistsAsync(
        string airlineIataCode, Result result, CancellationToken cancellationToken)
    {
        var airline = await _unitOfWork.Airlines.GetByIataCodeAsync(airlineIataCode, cancellationToken);
        if (airline == null)
        {
            var error = new Error
            {
                Message = $"Airline with IATA code {airlineIataCode} not found.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return airline;
    }

    private async Task<Aircraft?> ValidateAircraftExistsAsync(
        int aircraftId, Result result, CancellationToken cancellationToken)
    {
        var aircraft = await _unitOfWork.Aircrafts.GetByIdAsync(aircraftId, cancellationToken);
        if (aircraft == null)
        {
            var error = new Error
            {
                Message = $"Aircraft with ID {aircraftId} not found.",
                Type = ErrorType.NotFound
            };
            result.AddError(error);
        }
        return aircraft;
    }

    #endregion

    #region Logging Methods

    private void LogGetById(int aircraftId, bool found)
    {
        if (found)
        {
            _logger.LogDebug("Retrieved aircraft with ID {AircraftId}.", aircraftId);
        }
        else
        {
            _logger.LogDebug("Aircraft with ID {AircraftId} not found.", aircraftId);
        }
    }

    #endregion
}
