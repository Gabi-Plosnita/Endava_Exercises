using AirportTool.Domain;
using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class GateService : IGateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateGateDto> _createGateDtoValidator;
    private readonly IValidator<UpdateGateDto> _updateGateDtoValidator;
    private readonly ILogger<GateService> _logger;

    public GateService(IUnitOfWork unitOfWork,
                       IValidator<CreateGateDto> createGateDtoValidator,
                       IValidator<UpdateGateDto> updateGateDtoValidator,
                       ILogger<GateService> logger)
    {
        _unitOfWork = unitOfWork;
        _createGateDtoValidator = createGateDtoValidator;
        _updateGateDtoValidator = updateGateDtoValidator;
        _logger = logger;
    }

    public async Task<GetGateDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = new Result<GetGateDto?>();
        var getGateDto = await _unitOfWork.Gates.GetDtoByIdAsync(id, cancellationToken);
        return getGateDto;
    }

    public async Task<Result<GetGateDto?>> CreateAsync(CreateGateDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<GetGateDto?>();

        var dtoValidationResult = _createGateDtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            return result;
        }

        var airport = await ValidateAirportExistsAsync(dto.AirportIataCode, result, cancellationToken);
        if(airport != null)
        {
            await ValidateGateCodeIsUniqueForAirport(
                gateToUpdateId: null, airport.AirportId, dto.AirportIataCode, dto.Code, result, cancellationToken);
        }

        if (result.IsFailure)
        {
            return result;
        }

        var gate = new Gate
        {
            AirportId = airport!.AirportId,
            Code = dto.Code
        };

        await _unitOfWork.Gates.AddAndSaveAsync(gate, cancellationToken);
        var getGateDto = await _unitOfWork.Gates.GetDtoByIdAsync(gate.GateId, cancellationToken);
        result.Value = getGateDto;

        return result;
    }

    public Task<Result> UpdateAsync(int id, UpdateGateDto dto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = new Result();

        var gate = await ValidateGateExistsAsync(id, result, cancellationToken);
        if (result.IsFailure || gate == null)
        {
            return result;
        }

        await _unitOfWork.Gates.RemoveAsync(gate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        int? gateToUpdateId, int airportId, string airportIataCode, string code, Result result, CancellationToken cancellationToken)
    {
        var gate = await _unitOfWork.Gates.GetByAirlineIdAndCodeAsync(airportId, code, cancellationToken);
        if (gate != null && gateToUpdateId != gate.GateId)
        {
            var error = new Error
            {
                Message = $"Gate with Code {code} already exists for Airport {airportIataCode}.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }

    #endregion

    #region Logging Methods

    #endregion
}
