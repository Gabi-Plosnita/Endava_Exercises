using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateTicketDto> _createTicketDtoValidator;
    private readonly IValidator<UpdateTicketDto> _updateTicketDtoValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        IUnitOfWork unitOfWork, 
        IValidator<CreateTicketDto> createTicketDtoValidator,
        IValidator<UpdateTicketDto> updateTicketDtoValidator,
        ILogger<TicketService> logger,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _createTicketDtoValidator = createTicketDtoValidator;
        _updateTicketDtoValidator = updateTicketDtoValidator;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<GetTicketDto>>> GetByFlightScheduleIdAsync(int flightScheduleId, CancellationToken cancellationToken)
    {
        var result = new Result<IReadOnlyList<GetTicketDto>>();

        await ValidateFlightScheduleExistsAsync(flightScheduleId, result, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        var getTicketDtos = await _unitOfWork.Tickets.GetByFlightScheduleIdAsync(flightScheduleId, cancellationToken);
        result.Value = getTicketDtos;

        return result;
    }

    public async Task<Result<GetTicketDto?>> CreateAsync(CreateTicketDto dto, CancellationToken cancellationToken)
    {
        var result = new Result<GetTicketDto?>();

        var dtoValidationResult = _createTicketDtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if(result.IsFailure)
        {
            return result;
        }

        await ValidateFlightScheduleExistsAsync(dto.FlightScheduleId, result, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        var ticket = _mapper.Map<Ticket>(dto);
        await _unitOfWork.Tickets.AddAndSaveAsync(ticket, cancellationToken);
        var getTicketDto = await _unitOfWork.Tickets.GetDtoByIdAsync(ticket.TicketId, cancellationToken);
        result.Value = getTicketDto;

        return result;
    }

    public async Task<Result> UpdateAsync(long ticketId, UpdateTicketDto dto, CancellationToken cancellationToken)
    {
        var result = new Result();

        var dtoValidationResult = _updateTicketDtoValidator.Validate(dto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            return result;
        }

        var existingTicket = await ValidateTicketExistsAsync(ticketId, result, cancellationToken);
        if (result.IsFailure || existingTicket == null)
        {
            return result;
        }

        _mapper.Map(dto, existingTicket);
        await _unitOfWork.Tickets.UpdateAsync(existingTicket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result> DeleteByIdAsync(long ticketId, CancellationToken cancellationToken)
    {
        var result = new Result();

        var ticket = await ValidateTicketExistsAsync(ticketId, result, cancellationToken);
        if (result.IsFailure || ticket == null)
        {
            return result;
        }

        await ValidateTicketHasNoBookings(ticketId, result, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.Tickets.RemoveAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

    #endregion
}
