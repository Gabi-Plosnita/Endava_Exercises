using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDtoValidator _dtoValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        IUnitOfWork unitOfWork,
        IDtoValidator dtoValidator,
        ILogger<TicketService> logger,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _dtoValidator = dtoValidator;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<Result<GetTicketDto?>> GetByIdAsync(long ticketId, CancellationToken cancellationToken)
    {
        var result = new Result<GetTicketDto?>();
       
        var getTicketDto = await _unitOfWork.Tickets.GetDtoByIdAsync(ticketId, cancellationToken);
        var found = getTicketDto != null;

        if (!found)
        {
            result.AddError(new Error
            {
                Message = $"Ticket with ID {ticketId} not found.",
                Type = ErrorType.NotFound
            });
            return result;
        }

        LogGetById(ticketId, found);
        result.Value = getTicketDto;
        return result;
    }

    public async Task<Result<IReadOnlyList<GetTicketDto>>> GetByFlightScheduleIdAsync(
        int flightScheduleId, CancellationToken cancellationToken)
    {
        var result = new Result<IReadOnlyList<GetTicketDto>>();
        LogGetByFlightScheduleIdStart(flightScheduleId);

        await ValidateFlightScheduleExistsAsync(flightScheduleId, result, cancellationToken);
        if (result.IsFailure)
        {
            LogGetByFlightScheduleIdFailure(flightScheduleId, result);
            return result;
        }

        var getTicketDtos = await _unitOfWork.Tickets.GetByFlightScheduleIdAsync(flightScheduleId, cancellationToken);
        result.Value = getTicketDtos;

        LogGetByFlightScheduleIdSuccess(flightScheduleId, getTicketDtos?.Count ?? 0);
        return result;
    }

    public async Task<Result<GetTicketDto?>> CreateAsync(CreateTicketDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<GetTicketDto?>();
        LogCreateStart(dto);

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            LogCreateFailure(dto, result);
            return result;
        }

        await ValidateFlightScheduleExistsAsync(dto.FlightScheduleId, result, cancellationToken);
        if (result.IsFailure)
        {
            LogCreateFailure(dto, result);
            return result;
        }

        var ticket = _mapper.Map<Ticket>(dto);
        await _unitOfWork.Tickets.AddAndSaveAsync(ticket, cancellationToken);
        var getTicketDto = await _unitOfWork.Tickets.GetDtoByIdAsync(ticket.TicketId, cancellationToken);
        if(getTicketDto == null)
        {
            result.AddError(new Error
            {
                Message = "Ticket not found after creation.",
                Type = ErrorType.Unexpected
            });
            LogTicketNotFoundAfterCreation(ticket.TicketId);
            LogCreateFailure(dto, result);
            return result;
        }

        result.Value = getTicketDto;

        LogCreateSuccess(ticket);
        return result;
    }

    public async Task<Result> UpdateAsync(long ticketId, UpdateTicketDto dto, CancellationToken cancellationToken)
    {
        var result = new Result();
        LogUpdateStart(ticketId, dto);

        var dtoValidationResult = _dtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            LogUpdateFailure(ticketId, result);
            return result;
        }

        var existingTicket = await ValidateTicketExistsAsync(ticketId, result, cancellationToken);
        if (result.IsFailure || existingTicket == null)
        {
            LogUpdateFailure(ticketId, result);
            return result;
        }

        _mapper.Map(dto, existingTicket);
        await _unitOfWork.Tickets.UpdateAsync(existingTicket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogUpdateSuccess(ticketId);
        return result;
    }

    public async Task<Result> DeleteByIdAsync(long ticketId, CancellationToken cancellationToken)
    {
        var result = new Result();
        LogDeleteStart(ticketId);

        var ticket = await ValidateTicketExistsAsync(ticketId, result, cancellationToken);
        if (result.IsFailure || ticket == null)
        {
            LogDeleteFailure(ticketId, result);
            return result;
        }

        await ValidateTicketHasNoBookings(ticketId, result, cancellationToken);
        if (result.IsFailure)
        {
            LogDeleteFailure(ticketId, result);
            return result;
        }

        await _unitOfWork.Tickets.RemoveAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogDeleteSuccess(ticketId);
        return result;
    }

    #region Business Rules Methods

    private async Task ValidateFlightScheduleExistsAsync(int flightScheduleId, Result result, CancellationToken cancellationToken)
    {
        var flightSchedule = await _unitOfWork.FlightSchedules.GetByIdAsync(flightScheduleId, cancellationToken);
        if (flightSchedule == null)
        {
            var error = new Error
            {
                Message = $"Flight schedule with ID {flightScheduleId} does not exist.",
                Type = ErrorType.NotFound
            };
            result.AddError(error);
        }
    }

    private async Task<Ticket?> ValidateTicketExistsAsync(long ticketId, Result result, CancellationToken cancellationToken)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            var error = new Error
            {
                Message = $"Ticket with ID {ticketId} does not exist.",
                Type = ErrorType.NotFound
            };
            result.AddError(error);
        }
        return ticket;
    }

    private async Task ValidateTicketHasNoBookings(long ticketId, Result result, CancellationToken cancellationToken)
    {
        var hasBookings = await _unitOfWork.Tickets.HasBookingsAsync(ticketId, cancellationToken);
        if (hasBookings)
        {
            var error = new Error
            {
                Message = $"Ticket with ID {ticketId} has associated bookings and cannot be deleted.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }

    #endregion

    #region Logging Methods

    private void LogGetById(long ticketId, bool found)
    {
        if (found)
        {
            _logger.LogDebug(
                @"Retrieved ticket:
                    TicketId={TicketId}",
                ticketId);
        }
        else
        {
            _logger.LogDebug(
                @"Ticket not found:
                    TicketId={TicketId}",
                ticketId);
        }
    }

    private void LogGetByFlightScheduleIdStart(int flightScheduleId)
    {
        _logger.LogInformation(
            @"Retrieving tickets by flight schedule:
                FlightScheduleId={FlightScheduleId}",
            flightScheduleId);
    }

    private void LogGetByFlightScheduleIdFailure(int flightScheduleId, Result result)
    {
        _logger.LogWarning(
            @"Get tickets by flight schedule failed:
                FlightScheduleId={FlightScheduleId},
                Errors={Errors}",
            flightScheduleId,
            result.Errors);
    }

    private void LogGetByFlightScheduleIdSuccess(int flightScheduleId, int count)
    {
        _logger.LogDebug(
            "Retrieved {Count} ticket(s) for FlightScheduleId {FlightScheduleId}.",
            count,
            flightScheduleId);
    }

    private void LogCreateStart(CreateTicketDto dto)
    {
        _logger.LogInformation(
            @"Creating ticket:
                FlightScheduleId={FlightScheduleId},
                FareClass={FareClass},
                BasePrice={BasePrice},
                Taxes={Taxes},
                Currency={Currency},
                IsRefundable={IsRefundable},
                SeatInventory={SeatInventory}",
            dto.FlightScheduleId,
            dto.FareClass,
            dto.BasePrice,
            dto.Taxes,
            dto.Currency,
            dto.IsRefundable,
            dto.SeatInventory);
    }

    private void LogCreateFailure(CreateTicketDto dto, Result result)
    {
        _logger.LogWarning(
            @"Create ticket failed:
                FlightScheduleId={FlightScheduleId},
                FareClass={FareClass},
                Errors={Errors}",
            dto.FlightScheduleId,
            dto.FareClass,
            result.Errors);
    }

    private void LogCreateSuccess(Ticket ticket)
    {
        _logger.LogInformation(
            @"Ticket created successfully:
                TicketId={TicketId},
                FlightScheduleId={FlightScheduleId},
                FareClass={FareClass},
                Currency={Currency},
                SeatInventory={SeatInventory}",
            ticket.TicketId,
            ticket.FlightScheduleId,
            ticket.FareClass,
            ticket.Currency,
            ticket.SeatInventory);
    }

    private void LogUpdateStart(long ticketId, UpdateTicketDto dto)
    {
        _logger.LogInformation(
            @"Updating ticket:
                TicketId={TicketId},
                SeatInventory={SeatInventory}",
            ticketId,
            dto.SeatInventory);
    }

    private void LogUpdateFailure(long ticketId, Result result)
    {
        _logger.LogWarning(
            @"Update ticket failed:
                TicketId={TicketId},
                Errors={Errors}",
            ticketId,
            result.Errors);
    }

    private void LogUpdateSuccess(long ticketId)
    {
        _logger.LogInformation(
            @"Ticket updated successfully:
                TicketId={TicketId}",
            ticketId);
    }

    private void LogDeleteStart(long ticketId)
    {
        _logger.LogInformation(
            @"Deleting ticket:
                TicketId={TicketId}",
            ticketId);
    }

    private void LogDeleteFailure(long ticketId, Result result)
    {
        _logger.LogWarning(
            @"Delete ticket failed:
                TicketId={TicketId},
                Errors={Errors}",
            ticketId,
            result.Errors);
    }

    private void LogDeleteSuccess(long ticketId)
    {
        _logger.LogInformation(
            @"Ticket deleted successfully:
                TicketId={TicketId}",
            ticketId);
    }

    private void LogTicketNotFoundAfterCreation(long ticketId)
    {
        _logger.LogError(
            @"Ticket with ID {TicketId} was not found after creation.",
            ticketId);
    }

    #endregion
}
