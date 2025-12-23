using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class FlightService : IFlightService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDtoValidator _dtoValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<FlightService> _logger;

    public FlightService(IUnitOfWork unitOfWork, 
                         IDtoValidator dtoValidator, 
                         IMapper mapper,
                         ILogger<FlightService> logger)
    {
        _unitOfWork = unitOfWork;
        _dtoValidator = dtoValidator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<GetFlightDto?>> GetByIdAsync(int flightId, CancellationToken cancellationToken)
    {
        var result = new Result<GetFlightDto?>();

        var getFlightDto = await _unitOfWork.Flights.GetDtoByIdAsync(flightId, cancellationToken);
        var found = getFlightDto != null;

        if (!found)
        {
            result.AddError(new Error
            {
                Message = $"Flight with ID {flightId} not found.",
                Type = ErrorType.NotFound
            });
        }

        LogGetById(flightId, found);
        result.Value = getFlightDto;
        return result;
    }

    public async Task<Result<GetFlightDto?>> CreateAsync(CreateFlightDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<GetFlightDto?>();
        LogCreateStart(dto);

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            LogCreateFailure(dto, result);
            return result;
        }

        var airline = await ValidateAirlineExistsAsync(dto.AirlineIataCode, result, cancellationToken);
        if (airline != null)
        {
            await ValidateFlightIsUniqueForAirlineAsync(
                flightToUpdateId: null, 
                airlineId: airline.AirlineId, 
                airlineIataCode: airline.Iatacode, 
                flightNumber: dto.FlightNumber, 
                result, 
                cancellationToken);
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

        var flight = _mapper.Map<Flight>(dto);
        flight.AirlineId = airline!.AirlineId;
        flight.OriginAirportId = originAirport!.AirportId;
        flight.DestinationAirportId = destinationAirport!.AirportId;
        flight.DefaultAircraftId = defaultAircraft?.AircraftId;

        await _unitOfWork.Flights.AddAndSaveAsync(flight, cancellationToken);
        var getFlightDto = await _unitOfWork.Flights.GetDtoByIdAsync(flight.FlightId, cancellationToken);
        if(getFlightDto == null)
        {
            result.AddError(new Error
            {
                Message = "Flight not found after creation.",
                Type = ErrorType.Unexpected
            });
            LogFlightNotFoundAfterCreation(flight.FlightId);
            LogCreateFailure(dto, result);
            return result;
        }

        result.Value = getFlightDto;

        LogCreateSuccess(flight);
        return result;
    }

    public async Task<Result> UpdateAsync(int flightId, UpdateFlightDto dto, CancellationToken cancellationToken)
    {
        var result = new Result();
        LogUpdateStart(flightId, dto);

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if(result.IsFailure)
        {
            LogUpdateFailure(flightId, result);
            return result;
        }

        var existingFlight = await ValidateFlightExistsAsync(flightId, result, cancellationToken);
        if (result.IsFailure || existingFlight == null)
        {
            LogUpdateFailure(flightId, result);
            return result;
        }

        var airline = await ValidateAirlineExistsAsync(dto.AirlineIataCode, result, cancellationToken);
        if (airline != null)
        {
            await ValidateFlightIsUniqueForAirlineAsync(
                flightToUpdateId: flightId, 
                airlineId: airline.AirlineId, 
                airlineIataCode: airline.Iatacode, 
                flightNumber: dto.FlightNumber, 
                result, 
                cancellationToken);
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

        _mapper.Map(dto, existingFlight);
        existingFlight.AirlineId = airline!.AirlineId;
        existingFlight.OriginAirportId = originAirport!.AirportId;
        existingFlight.DestinationAirportId = destinationAirport!.AirportId;
        existingFlight.DefaultAircraftId = defaultAircraft?.AircraftId;

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

    #region Business Rules Methods

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
        int? flightToUpdateId, int airlineId, string airlineIataCode, string flightNumber, Result result, CancellationToken cancellationToken)
    {
        var existingFlight = await _unitOfWork.Flights.GetByAirlineIdAndFlightNumberAsync(airlineId, flightNumber, cancellationToken);

        if (existingFlight != null && existingFlight.FlightId != flightToUpdateId)
        {
            var error = new Error
            {
                Message = $"Flight with Number {flightNumber} already exists for Airline {airlineIataCode}",
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

    private void LogFlightNotFoundAfterCreation(int flightId)
    {
        _logger.LogError(
            @"Flight with ID {FlightId} not found after creation.",
            flightId);
    }
    #endregion
}
