using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AirportTool.Application;

public class FlightService : IFlightService
{
    private readonly ILogger<FlightService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FlightService(ILogger<FlightService> logger, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _logger = logger;
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
        if (airline != null)
        {
            await ValidateFlightIsUniqueForAirlineAsync(null, airline.Id, dto.FlightNumber, result, cancellationToken);
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

    public async Task<Result> UpdateAsync(int flightId, UpdateFlightDto dto, CancellationToken cancellationToken)
    {
        var result = new Result();

        var flight = await ValidateFlightExistsAsync(flightId, result, cancellationToken);
        if (result.IsFailure || flight == null)
        {
            return result;
        }

        ValidateFlightNumber(dto.FlightNumber, result);
        ValidateOriginAndDestinationAirportsAreDifferent(dto.OriginAirportIataCode, dto.DestinationAirportIataCode, result);

        var airline = await ValidateAirlineExistsAsync(dto.AirlineIataCode, result, cancellationToken);
        if (airline != null)
        {
            await ValidateFlightIsUniqueForAirlineAsync(flightId, airline.Id, dto.FlightNumber, result, cancellationToken);
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

        flight.FlightNumber = dto.FlightNumber;
        flight.AirlineId = airline!.Id;
        flight.OriginAirportId = originAirport!.Id;
        flight.DestinationAirportId = destinationAirport!.Id;
        flight.DefaultAircraftId = defaultAircraftTail?.Id;
        flight.IsActive = dto.IsActive;

        await _unitOfWork.Flights.UpdateAsync(flight, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<Result> DeleteByIdAsync(int flightId, CancellationToken cancellationToken)
    {
        var result = new Result();

        var flight = await ValidateFlightExistsAsync(flightId, result, cancellationToken);
        if (result.IsFailure || flight == null)
        {
            return result;
        }

        await ValidateFlightHasNoAssociatedFlightSchedulesAsync(flightId, result, cancellationToken);
        if (result.IsFailure)
        {
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
            var error = new Error
            {
                Message = "FlightNumber is required.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
            return;
        }
        if (!Regex.IsMatch(flightNumber, @"^[A-Za-z]+[0-9]+$"))
        {
            var error = new Error
            {
                Message = "FlightNumber must be letters followed by numbers.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }

    private void ValidateOriginAndDestinationAirportsAreDifferent(string originIataCode, string destinationIataCode, Result result)
    {
        if (originIataCode == destinationIataCode)
        {
            var error = new Error
            {
                Message = "Origin and Destination airports must be different.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }

    private async Task<Airline?> ValidateAirlineExistsAsync(string iataCode, Result result, CancellationToken cancellationToken)
    {
        var airline = await _unitOfWork.Airlines.GetByIataCodeAsync(iataCode, cancellationToken);
        if (airline == null)
        {
            var error = new Error
            {
                Message = $"Airline with IataCode {iataCode} not found.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return airline;
    }

    private async Task ValidateFlightIsUniqueForAirlineAsync(
        int? currentFlightId, int airlineId, string flightNumber, Result result, CancellationToken cancellationToken)
    {
        var existingFlight = await _unitOfWork.Flights.GetByAirlineIdAndFlightNumberAsync(airlineId, flightNumber, cancellationToken);

        if (existingFlight != null && existingFlight.Id != currentFlightId)
        {
            var error = new Error
            {
                Message = "Another flight with the same FlightNumber already exists for this airline.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }


    private async Task<Flight?> ValidateFlightExistsAsync(int flightId, Result result, CancellationToken cancellationToken)
    {
        var flight = await _unitOfWork.Flights.GetByIdAsync(flightId, cancellationToken);
        if (flight == null)
        {
            var error = new Error
            {
                Message = $"Flight with ID {flightId} not found.",
                Type = ErrorType.NotFound
            };
            result.AddError(error);
        }
        return flight;
    }

    private async Task<Airport?> ValidateAirportExistsAsync(string iataCode, Result result, CancellationToken cancellationToken)
    {
        var airport = await _unitOfWork.Airports.GetByIataCodeAsync(iataCode, cancellationToken);
        if (airport == null)
        {
            var error = new Error
            {
                Message = $"Airport with IataCode {iataCode} not found.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return airport;
    }

    private async Task<Aircraft?> ValidateAircraftExistsAsync(string tailNumber, Result result, CancellationToken cancellationToken)
    {
        var aircraft = await _unitOfWork.Aircrafts.GetByTailNumberAsync(tailNumber, cancellationToken);
        if (aircraft == null)
        {
            var error = new Error
            {
                Message = $"Aircraft with TailNumber {tailNumber} not found.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return aircraft;
    }

    private async Task ValidateFlightHasNoAssociatedFlightSchedulesAsync(int flightId, Result result, CancellationToken cancellationToken)
    {
        var hasAssociatedSchedules = await _unitOfWork.Flights.HasAnyFlightSchedulesAsync(flightId, cancellationToken);
        if (hasAssociatedSchedules)
        {
            var error = new Error
            {
                Message = $"Flight with ID {flightId} cannot be deleted because it has associated schedules.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }

    private void LogCreateStart(CreateFlightDto dto)
    {
        _logger.LogInformation(
            "Creating flight {@FlightInfo}",
             new
             {
                 dto.AirlineIataCode,
                 dto.FlightNumber,
                 dto.OriginAirportIataCode,
                 dto.DestinationAirportIataCode,
                 dto.DefaultAircraftTail,
                 dto.IsActive
             });
    }

    private void LogCreateFailure(CreateFlightDto dto, Result result)
    {
        _logger.LogWarning(
            "Create flight failed for Airline={AirlineIata}, FlightNumber={FlightNumber}. Errors: {@Errors}",
            dto.AirlineIataCode,
            dto.FlightNumber,
            result.Errors);
    }

    private void LogCreateSuccess(Flight flight)
    {
        _logger.LogInformation(
            "Flight created successfully {@FlightInfo}",
            new
            {
                flight.Id,
                flight.AirlineId,
                flight.FlightNumber,
                flight.OriginAirportId,
                flight.DestinationAirportId
            });
    }

    private void LogUpdateStart(int flightId, UpdateFlightDto dto)
    {
        _logger.LogInformation(
            "Updating flight {@FlightUpdateInfo}",
            new
            {
                FlightId = flightId,
                dto.AirlineIataCode,
                dto.FlightNumber,
                dto.OriginAirportIataCode,
                dto.DestinationAirportIataCode,
                dto.DefaultAircraftTail,
                dto.IsActive
            });
    }

    private void LogUpdateFailure(int flightId, Result result)
    {
        _logger.LogWarning(
            "Update flight failed for FlightId={FlightId}. Errors: {@Errors}",
            flightId,
            result.Errors);
    }

    private void LogUpdateSuccess(Flight flight)
    {
        _logger.LogInformation(
            "Flight updated successfully: FlightId={FlightId}, AirlineId={AirlineId}, FlightNumber={FlightNumber}",
            flight.Id,
            flight.AirlineId,
            flight.FlightNumber);
    }

    private void LogDeleteStart(int flightId)
    {
        _logger.LogInformation("Deleting flight with ID {FlightId}.", flightId);
    }

    private void LogDeleteFailure(int flightId, Result result)
    {
        _logger.LogWarning(
            "Delete flight failed for FlightId={FlightId}. Errors: {@Errors}",
            flightId,
            result.Errors);
    }

    private void LogDeleteSuccess(int flightId)
    {
        _logger.LogInformation("Flight deleted successfully: FlightId={FlightId}", flightId);
    }
}
