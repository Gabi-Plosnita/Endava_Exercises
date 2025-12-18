using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AirportTool.Application;

public class FlightService : IFlightService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<FlightService> _logger;

    public FlightService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<FlightService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GetFlightDto?> GetByIdAsync(int flightId, CancellationToken cancellationToken)
    {
        var getFlightDto = await _unitOfWork.Flights.GetDtoByIdAsync(flightId, cancellationToken);
        var found = getFlightDto != null;
        LogGetById(flightId, found);
        return getFlightDto;
    }

    public async Task<Result<GetFlightDto?>> CreateAsync(CreateFlightDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<GetFlightDto?>();
        LogCreateStart(dto);

        ValidateFlightNumber(dto.FlightNumber, result);
        ValidateOriginAndDestinationAirportsAreDifferent(dto.OriginAirportIataCode, dto.DestinationAirportIataCode, result);

        var airline = await ValidateAirlineExistsAsync(dto.AirlineIataCode, result, cancellationToken);
        if (airline != null)
        {
            await ValidateFlightIsUniqueForAirlineAsync(null, airline.AirlineId, dto.FlightNumber, result, cancellationToken);
        }

        var originAirport = await ValidateAirportExistsAsync(dto.OriginAirportIataCode, result, cancellationToken);
        var destinationAirport = await ValidateAirportExistsAsync(dto.DestinationAirportIataCode, result, cancellationToken);

        Aircraft? defaultAircraft = null;
        if (dto.DefaultAircraftTail != null)
        {
            defaultAircraft = await ValidateAircraftExistsAsync(dto.DefaultAircraftTail, result, cancellationToken);
        }

        if (result.IsFailure)
        {
            LogCreateFailure(dto, result);
            return result;
        }

        var flight = new Flight
        {
            FlightNumber = dto.FlightNumber,
            AirlineId = airline!.AirlineId,
            OriginAirportId = originAirport!.AirportId,
            DestinationAirportId = destinationAirport!.AirportId,
            DefaultAircraftId = defaultAircraft?.AircraftId,
            IsActive = dto.IsActive
        };

        await _unitOfWork.Flights.AddAndSaveAsync(flight, cancellationToken);
        var getFlightDto = await _unitOfWork.Flights.GetDtoByIdAsync(flight.FlightId, cancellationToken);
        result.Value = getFlightDto;

        LogCreateSuccess(flight);
        return result;
    }

    public async Task<Result> UpdateAsync(int flightId, UpdateFlightDto dto, CancellationToken cancellationToken)
    {
        var result = new Result();
        LogUpdateStart(flightId, dto);

        var existingFlight = await ValidateFlightExistsAsync(flightId, result, cancellationToken);
        if (result.IsFailure || existingFlight == null)
        {
            LogUpdateFailure(flightId, result);
            return result;
        }

        ValidateFlightNumber(dto.FlightNumber, result);
        ValidateOriginAndDestinationAirportsAreDifferent(dto.OriginAirportIataCode, dto.DestinationAirportIataCode, result);

        var airline = await ValidateAirlineExistsAsync(dto.AirlineIataCode, result, cancellationToken);
        if (airline != null)
        {
            await ValidateFlightIsUniqueForAirlineAsync(flightId, airline.AirlineId, dto.FlightNumber, result, cancellationToken);
        }

        var originAirport = await ValidateAirportExistsAsync(dto.OriginAirportIataCode, result, cancellationToken);
        var destinationAirport = await ValidateAirportExistsAsync(dto.DestinationAirportIataCode, result, cancellationToken);

        Aircraft? defaultAircraft = null;
        if (dto.DefaultAircraftTail != null)
        {
            defaultAircraft = await ValidateAircraftExistsAsync(dto.DefaultAircraftTail, result, cancellationToken);
        }

        if (result.IsFailure)
        {
            LogUpdateFailure(flightId, result);
            return result;
        }

        existingFlight.FlightNumber = dto.FlightNumber;
        existingFlight.AirlineId = airline!.AirlineId;
        existingFlight.OriginAirportId = originAirport!.AirportId;
        existingFlight.DestinationAirportId = destinationAirport!.AirportId;
        existingFlight.DefaultAircraftId = defaultAircraft?.AircraftId;
        existingFlight.IsActive = dto.IsActive;

        await _unitOfWork.Flights.UpdateAsync(existingFlight, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogUpdateSuccess(existingFlight);
        return result;
    }

    public async Task<Result> DeleteByIdAsync(int flightId, CancellationToken cancellationToken)
    {
        var result = new Result();
        LogDeleteStart(flightId);

        var flight = await ValidateFlightExistsAsync(flightId, result, cancellationToken);
        if (result.IsFailure || flight == null)
        {
            LogDeleteFailure(flightId, result);
            return result;
        }

        await ValidateFlightHasNoAssociatedFlightSchedulesAsync(flightId, result, cancellationToken);
        if (result.IsFailure)
        {
            LogDeleteFailure(flightId, result);
            return result;
        }

        await _unitOfWork.Flights.RemoveAsync(flight, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogDeleteSuccess(flightId);
        return result;
    }

    #region Validator Methods

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

        if (existingFlight != null && existingFlight.FlightId != currentFlightId)
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

    #endregion

    #region Logging Methods
    private void LogGetById(int flightId, bool found)
    {
        if (found)
        {
            _logger.LogDebug("Retrieved flight with ID {FlightId}.", flightId);
        }
        else
        {
            _logger.LogDebug("Flight with ID {FlightId} not found.", flightId);
        }
    }

    private void LogCreateStart(CreateFlightDto dto)
    {
        _logger.LogInformation(
            @"Creating flight:
                Airline={AirlineIata},
                FlightNumber={FlightNumber},
                Origin={Origin},
                Destination={Destination},
                DefaultAircraftTail={DefaultTail},
                IsActive={IsActive}",
            dto.AirlineIataCode,
            dto.FlightNumber,
            dto.OriginAirportIataCode,
            dto.DestinationAirportIataCode,
            dto.DefaultAircraftTail,
            dto.IsActive);
    }

    private void LogCreateFailure(CreateFlightDto dto, Result result)
    {
        _logger.LogWarning(
            @"Create flight failed:
                Airline={AirlineIata},
                FlightNumber={FlightNumber},
                Errors={Errors}",
            dto.AirlineIataCode,
            dto.FlightNumber,
            result.Errors);
    }

    private void LogCreateSuccess(Flight flight)
    {
        _logger.LogInformation(
            @"Flight created successfully:
                FlightId={FlightId},
                AirlineId={AirlineId},
                FlightNumber={FlightNumber},
                OriginId={OriginId},
                DestinationId={DestinationId},
                DefaultAircraftId={DefaultAircraftId},
                IsActive={IsActive}",
            flight.FlightId,
            flight.AirlineId,
            flight.FlightNumber,
            flight.OriginAirportId,
            flight.DestinationAirportId,
            flight.DefaultAircraftId,
            flight.IsActive);
    }


    private void LogUpdateStart(int flightId, UpdateFlightDto dto)
    {
        _logger.LogInformation(
            @"Updating flight:
                FlightId={FlightId},
                Airline={AirlineIata},
                FlightNumber={FlightNumber},
                Origin={Origin},
                Destination={Destination},
                DefaultAircraftTail={DefaultTail},
                IsActive={IsActive}",
            flightId,
            dto.AirlineIataCode,
            dto.FlightNumber,
            dto.OriginAirportIataCode,
            dto.DestinationAirportIataCode,
            dto.DefaultAircraftTail,
            dto.IsActive);
    }


    private void LogUpdateFailure(int flightId, Result result)
    {
        _logger.LogWarning(
            @"Update flight failed:
                FlightId={FlightId},
                Errors={Errors}",
            flightId,
            result.Errors);
    }

    private void LogUpdateSuccess(Flight flight)
    {
        _logger.LogInformation(
            @"Flight updated successfully:
                FlightId={FlightId},
                AirlineId={AirlineId},
                FlightNumber={FlightNumber}",
            flight.FlightId,
            flight.AirlineId,
            flight.FlightNumber);
    }

    private void LogDeleteStart(int flightId)
    {
        _logger.LogInformation(
            @"Deleting flight:
                FlightId={FlightId}",
            flightId);
    }

    private void LogDeleteFailure(int flightId, Result result)
    {
        _logger.LogWarning(
            @"Delete flight failed:
                FlightId={FlightId},
                Errors={Errors}",
            flightId,
            result.Errors);
    }

    private void LogDeleteSuccess(int flightId)
    {
        _logger.LogInformation(
            @"Flight deleted successfully:
                FlightId={FlightId}",
            flightId);
    }

    #endregion
}
