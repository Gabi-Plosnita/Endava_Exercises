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

        var flightValidationResult = await ValidateAndBuildFlightAsync(dto, cancellationToken);
        var flight = flightValidationResult.Value;
        if (flightValidationResult.IsFailure || flight == null)
        {
            result.AddErrors(flightValidationResult.Errors);
            return result;
        }

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

    private async Task<Result<Flight?>> ValidateAndBuildFlightAsync(CreateFlightDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<Flight?>();

        if (IsValidFlightNumber(dto.FlightNumber))
        {
            result.AddError($"FlightNumber '{dto.FlightNumber}' has invalid format. It must be letters followed by numbers.");
        }

        if (dto.OriginAirportIataCode == dto.DestinationAirportIataCode)
        {
            result.AddError("Origin and Destination airports must be different");
        }

        var airline = await _unitOfWork.Airlines.GetByIataCodeAsync(dto.AirlineIataCode, cancellationToken);
        if (airline == null)
        {
            result.AddError($"Airline with IataCode {dto.AirlineIataCode} not found");
        }
        else
        {
            var existingFlight = await _unitOfWork.Flights.GetByAirlineIdAndFlightNumberAsync(airline.Id, dto.FlightNumber, cancellationToken);

            if (existingFlight != null)
            {
                result.AddError($"Flight with FlightNumber {dto.FlightNumber} already exists for Airline {dto.AirlineIataCode}");
            }
        }

        var originAirport = await _unitOfWork.Airports.GetByIataCodeAsync(dto.OriginAirportIataCode, cancellationToken);
        if (originAirport == null)
        {
            result.AddError($"Airport with IataCode {dto.OriginAirportIataCode} not found");
        }

        var destinationAirport = await _unitOfWork.Airports.GetByIataCodeAsync(dto.DestinationAirportIataCode, cancellationToken);
        if (destinationAirport == null)
        {
            result.AddError($"Airport with IataCode {dto.DestinationAirportIataCode} not found");
        }

        Aircraft? defaultAircraftTail = null;
        if (dto.DefaultAircraftTail != null)
        {
            defaultAircraftTail = await _unitOfWork.Aircrafts.GetByTailNumberAsync(dto.DefaultAircraftTail, cancellationToken);
            if (defaultAircraftTail == null)
            {
                result.AddError($"Aircraft with TailNumber {dto.DefaultAircraftTail} not found");
            }
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
        result.Value = flight;
        return result;
    }

    private bool IsValidFlightNumber(string flightNumber)
    {
        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            return false;
        }
        return Regex.IsMatch(flightNumber, @"^[A-Za-z]+[0-9]+$");
    }
}
