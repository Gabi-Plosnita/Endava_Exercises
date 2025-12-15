using AutoMapper;

namespace AirportTool.Application;

public class FlightService : IFlightService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FlightService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<GetFlightDto?> GetByIdAsync(int flightId, CancellationToken cancellationToken)
    {
        var flight = await _unitOfWork.Flights.GetByIdAsync(flightId, cancellationToken);
        var getFlightDto = _mapper.Map<GetFlightDto>(flight);
        return getFlightDto;
    }

    public async Task<Result<GetFlightDto?>> CreateAsync(CreateFlightDto dto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> UpdateAsync(int flightId, CreateFlightDto dto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteByIdAsync(int flightId, CancellationToken cancellationToken)
    {
        var result = new Result();

        var flight = await _unitOfWork.Flights.GetByIdAsync(flightId, cancellationToken);
        if (flight == null)
        {
            result.AddError($"Flight with ID {flightId} not found.");
            return result;
        }

        await _unitOfWork.Flights.RemoveAsync(flight, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
