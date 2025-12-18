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

    public Task<Result<GetGateDto?>> CreateAsync(CreateGateDto dto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result> UpdateAsync(int id, UpdateGateDto dto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result> DeleteByIdAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    #region Business Rules Methods

    #endregion

    #region Logging Methods

    #endregion
}
