using AirportTool.Domain;
using AutoMapper;
using System.Text.RegularExpressions;

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
        var result = new Result<GetFlightDto?>();

        ValidateFlightNumber(dto.FlightNumber, result);
        ValidateOriginAndDestinationAirportsAreDifferent(dto.OriginAirportIataCode, dto.DestinationAirportIataCode, result);

        var airline = await ValidateAirlineExistsAsync(dto.AirlineIataCode, result, cancellationToken);
        if(airline != null)
        {
            await ValidateFlightDoesNotExistAsync(airline.Id, dto.FlightNumber, result, cancellationToken);
        }

        var originAirport = await ValidateAirportExistsAsync(dto.OriginAirportIataCode, result, cancellationToken);
        var destinationAirport = await ValidateAirportExistsAsync(dto.DestinationAirportIataCode, result, cancellationToken);

        Aircraft? defaultAircraftTail = null;
        if (dto.DefaultAircraftTail != null)
        {
            defaultAircraftTail = await ValidateAircraftExistsAsync(dto.DefaultAircraftTail, result, cancellationToken);
        }

        if (result.IsFailure)
        {
            return result;
        }

        var flight = new Flight
        {
            FlightNumber = dto.FlightNumber,
            AirlineId = airline!.Id,
            OriginAirportId = originAirport!.Id,
            DestinationAirportId = destinationAirport!.Id,
            DefaultAircraftId = defaultAircraftTail?.Id,
            IsActive = dto.IsActive
        };

        await _unitOfWork.Flights.AddAsync(flight, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        result.Value = _mapper.Map<GetFlightDto>(flight);
        return result;
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

        var hasAssociatedSchedules = await _unitOfWork.Flights.HasAnyFlightSchedulesAsync(flightId, cancellationToken);
        if (hasAssociatedSchedules)
        {
            result.AddError($"Flight with ID {flightId} cannot be deleted because it has associated schedules.");
            return result;
        }

        await _unitOfWork.Flights.RemoveAsync(flight, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    private void ValidateFlightNumber(string flightNumber, Result result)
    {
        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            result.AddError("FlightNumber is required.");
            return;
        }
        if (!Regex.IsMatch(flightNumber, @"^[A-Za-z]+[0-9]+$"))
        {
            result.AddError("FlightNumber must be letters followed by numbers.");
        }
    }

    private void ValidateOriginAndDestinationAirportsAreDifferent(string originIataCode, string destinationIataCode, Result result)
    {
        if (originIataCode == destinationIataCode)
        {
            result.AddError("Origin and Destination airports must be different.");
        }
    }

    private async Task<Airline?> ValidateAirlineExistsAsync(string iataCode, Result result, CancellationToken cancellationToken)
    {
        var airline = await _unitOfWork.Airlines.GetByIataCodeAsync(iataCode, cancellationToken);
        if (airline == null)
        {
            result.AddError($"Airline with IataCode {iataCode} not found.");
        }
        return airline;
    }

    private async Task ValidateFlightDoesNotExistAsync(int airlineId, string flightNumber, Result result, CancellationToken cancellationToken)
    {
        var flight = await _unitOfWork.Flights.GetByAirlineIdAndFlightNumberAsync(airlineId, flightNumber, cancellationToken);
        if(flight != null)
        {
            result.AddError("Flight already exists");
        }
    }

    private async Task<Airport?> ValidateAirportExistsAsync(string iataCode, Result result, CancellationToken cancellationToken)
    {
        var airport = await _unitOfWork.Airports.GetByIataCodeAsync(iataCode, cancellationToken);
        if (airport == null)
        {
            result.AddError($"Airport with IataCode {iataCode} not found.");
        }
        return airport;
    }

    private async Task<Aircraft?> ValidateAircraftExistsAsync(string tailNumber, Result result, CancellationToken cancellationToken)
    {
        var aircraft = await _unitOfWork.Aircrafts.GetByTailNumberAsync(tailNumber, cancellationToken);
        if(aircraft == null)
        {
            result.AddError($"Aircraft with TailNumber {tailNumber} not found");
        }
        return aircraft;
    }
}
