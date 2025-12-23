using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AirportTool.Application;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDtoValidator _dtoValidator;
    private readonly IUniqueCodeGenerator _codeGenerator;
    private readonly IMapper _mapper;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IUnitOfWork unitOfWork,
        IDtoValidator dtoValidator,
        IUniqueCodeGenerator codeGenerator,
        IMapper mapper,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork;
        _dtoValidator = dtoValidator;
        _codeGenerator = codeGenerator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<GetBookingDto?>> GetBookingByCodeAsync(string confirmationCode, CancellationToken cancellationToken)
    {
        var result = new Result<GetBookingDto?>();

        var booking = await ValidateBookingExistsAsync(confirmationCode, result, cancellationToken);
        var found = booking != null;

        LogGetByCode(confirmationCode, found);

        if (result.IsFailure || booking == null)
        {
            LogGetByCodeFailure(confirmationCode, result);
            return result;
        }

        var ticket = await _unitOfWork.Tickets.GetByIdAsync(booking.TicketId, cancellationToken);
        if (ticket == null)
        {
            result.AddError(new Error
            {
                Message = $"Ticket with ID '{booking.TicketId}' not found for booking '{confirmationCode}'.",
                Type = ErrorType.Unexpected
            });
            LogTicketMissingForBooking(booking.TicketId, confirmationCode, "get booking");
            LogGetByCodeFailure(confirmationCode, result);
            return result;
        }

        var getBookingDto = _mapper.Map<GetBookingDto>(booking);
        getBookingDto.TotalPrice = CalculateTotalPrice(ticket.BasePrice, ticket.Taxes, booking.Quantity);
        result.Value = getBookingDto;

        return result;
    }

    public async Task<Result<GetBookingDto?>> CreateBookingAsync(CreateBookingDto createBookingDto, CancellationToken cancellationToken)
    {
        var result = new Result<GetBookingDto?>();

        LogCreateStart(createBookingDto);

        var dtoValidationResult = _dtoValidator.Validate(createBookingDto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            LogCreateFailure(createBookingDto, result);
            return result;
        }

        var ticket = await ValidateTicketExistsAsync(createBookingDto.TicketId, result, cancellationToken);
        if (result.IsFailure || ticket == null)
        {
            LogCreateFailure(createBookingDto, result);
            return result;
        }

        ValidateSeatAvailability(createBookingDto.Quantity, ticket.SeatInventory, result);
        if (result.IsFailure)
        {
            LogCreateFailure(createBookingDto, result);
            return result;
        }

        var booking = _mapper.Map<Booking>(createBookingDto);
        booking.ConfirmationCode = _codeGenerator.Generate();
        booking.Status = BookingStatus.Active;

        ticket.SeatInventory -= createBookingDto.Quantity;

        await _unitOfWork.Bookings.AddAsync(booking, cancellationToken);
        await _unitOfWork.Tickets.UpdateAsync(ticket, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            LogCreateConflict(createBookingDto, booking.ConfirmationCode);
            result.AddError(new Error
            {
                Type = ErrorType.Conflict,
                Message = "Seat availability changed while processing your request. Please retry."
            });
            return result;
        }

        var createdBooking = await ValidateCreatedBookingExists(booking.ConfirmationCode, result, cancellationToken);
        if (result.IsFailure || createdBooking == null)
        {
            LogCreatePostSaveNotFound(booking.ConfirmationCode);
            return result;
        }

        var getBookingDto = _mapper.Map<GetBookingDto>(createdBooking);
        getBookingDto.TotalPrice = CalculateTotalPrice(ticket.BasePrice, ticket.Taxes, createBookingDto.Quantity);
        result.Value = getBookingDto;

        LogCreateSuccess(createdBooking, createBookingDto.Quantity);
        return result;
    }

    public async Task<Result> CancelBookingAsync(string confirmationCode, CancellationToken cancellationToken)
    {
        var result = new Result();

        LogCancelStart(confirmationCode);

        var booking = await ValidateBookingExistsAsync(confirmationCode, result, cancellationToken);
        if (result.IsFailure || booking == null)
        {
            LogCancelFailure(confirmationCode, result);
            return result;
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            LogCancelAlreadyCancelled(confirmationCode);
            return result;
        }

        var ticket = await ValidateTicketExistsAsync(booking.TicketId, result, cancellationToken);
        if (ticket == null)
        {
            result.AddError(new Error
            {
                Message = $"Ticket with ID '{booking.TicketId}' not found for booking '{confirmationCode}'.",
                Type = ErrorType.Unexpected
            });
            LogTicketMissingForBooking(booking.TicketId, confirmationCode, "cancel booking");
            LogCancelFailure(confirmationCode, result);
            return result;
        }

        booking.Status = BookingStatus.Cancelled;
        ticket.SeatInventory += booking.Quantity;

        await _unitOfWork.Bookings.UpdateAsync(booking, cancellationToken);
        await _unitOfWork.Tickets.UpdateAsync(ticket, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            LogCancelConflict(confirmationCode, booking.BookingId, booking.TicketId);

            result.AddError(new Error
            {
                Type = ErrorType.Conflict,
                Message = "Booking or ticket was updated by another request. Please retry."
            });

            LogCancelFailure(confirmationCode, result);
            return result;
        }

        LogCancelSuccess(confirmationCode, booking.BookingId, booking.TicketId, booking.Quantity);
        return result;
    }

    private decimal CalculateTotalPrice(decimal basePrice, decimal taxes, int quantity)
    {
        return (basePrice + taxes) * quantity;
    }

    #region Business Rules Methods

    private async Task<Booking?> ValidateBookingExistsAsync(string confirmationCode, Result result, CancellationToken cancellationToken)
    {
        var booking = await _unitOfWork.Bookings.GetByConfirmationCodeAsync(confirmationCode, cancellationToken);
        if (booking == null)
        {
            var error = new Error
            {
                Message = $"Booking with confirmation code '{confirmationCode}' not found.",
                Type = ErrorType.NotFound
            };
            result.AddError(error);
        }
        return booking;
    }

    private async Task<Ticket?> ValidateTicketExistsAsync(long ticketId, Result result, CancellationToken cancellationToken)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            var error = new Error
            {
                Message = $"Ticket with ID '{ticketId}' not found.",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
        return ticket;
    }

    private void ValidateSeatAvailability(int quantity, int seatInventory, Result result)
    {
        if (quantity > seatInventory)
        {
            var seatWord = seatInventory == 1 ? "seat" : "seats";
            var error = new Error
            {
                Message = $"Only {seatInventory} {seatWord} available, but {quantity} were requested",
                Type = ErrorType.Validation
            };
            result.AddError(error);
        }
    }

    private async Task<Booking?> ValidateCreatedBookingExists(string confirmationCode, Result result, CancellationToken cancellationToken)
    {
        var booking = await _unitOfWork.Bookings.GetByConfirmationCodeAsync(confirmationCode, cancellationToken);
        if(booking == null)
        {
            var error = new Error
            {
                Message = "Booking not found after creation.",
                Type = ErrorType.Unexpected
            };
            result.AddError(error);
        }
        return booking;
    }

    #endregion

    #region Logging Methods

    private void LogGetByCode(string confirmationCode, bool found)
    {
        if (found)
        {
            _logger.LogDebug("Retrieved booking with confirmation code {ConfirmationCode}.", confirmationCode);
        }
        else
        {
            _logger.LogDebug("Booking with confirmation code {ConfirmationCode} not found.", confirmationCode);
        }
    }

    private void LogGetByCodeFailure(string confirmationCode, Result result)
    {
        _logger.LogWarning(
            @"Get booking failed:
                ConfirmationCode={ConfirmationCode},
                Errors={Errors}",
            confirmationCode,
            result.Errors);
    }

    private void LogCreateStart(CreateBookingDto dto)
    {
        _logger.LogInformation(
            @"Creating booking:
                TicketId={TicketId},
                PassengerFullName={PassengerFullName},
                PassengerEmail={PassengerEmail},
                Quantity={Quantity}",
            dto.TicketId,
            dto.PassengerFullName,
            dto.PassengerEmail,
            dto.Quantity);
    }

    private void LogCreateFailure(CreateBookingDto dto, Result result)
    {
        _logger.LogWarning(
            @"Create booking failed:
                TicketId={TicketId},
                PassengerEmail={PassengerEmail},
                Quantity={Quantity},
                Errors={Errors}",
            dto.TicketId,
            dto.PassengerEmail,
            dto.Quantity,
            result.Errors);
    }

    private void LogCreatePostSaveNotFound(string confirmationCode)
    {
        _logger.LogError(
            @"Booking creation inconsistency detected:
            ConfirmationCode={ConfirmationCode}
            Reason=Booking not found after successful save operation",
            confirmationCode);
    }

    private void LogCreateConflict(CreateBookingDto dto, string confirmationCode)
    {
        _logger.LogWarning(
            @"Create booking concurrency conflict:
                TicketId={TicketId},
                PassengerEmail={PassengerEmail},
                Quantity={Quantity},
                GeneratedConfirmationCode={ConfirmationCode}",
            dto.TicketId,
            dto.PassengerEmail,
            dto.Quantity,
            confirmationCode);
    }

    private void LogCreateSuccess(Booking booking, int quantity)
    {
        _logger.LogInformation(
            @"Booking created successfully:
                BookingId={BookingId},
                TicketId={TicketId},
                ConfirmationCode={ConfirmationCode},
                PassengerEmail={PassengerEmail},
                Quantity={Quantity},
                Status={Status}",
            booking.BookingId,
            booking.TicketId,
            booking.ConfirmationCode,
            booking.PassengerEmail,
            quantity,
            booking.Status);
    }

    private void LogCancelStart(string confirmationCode)
    {
        _logger.LogInformation(
            @"Cancelling booking:
                ConfirmationCode={ConfirmationCode}",
            confirmationCode);
    }

    private void LogCancelAlreadyCancelled(string confirmationCode)
    {
        _logger.LogDebug(
            @"Cancel booking no-op (already cancelled):
                ConfirmationCode={ConfirmationCode}",
            confirmationCode);
    }

    private void LogCancelFailure(string confirmationCode, Result result)
    {
        _logger.LogWarning(
            @"Cancel booking failed:
                ConfirmationCode={ConfirmationCode},
                Errors={Errors}",
            confirmationCode,
            result.Errors);
    }

    private void LogCancelConflict(string confirmationCode, long bookingId, long ticketId)
    {
        _logger.LogWarning(
            @"Cancel booking concurrency conflict:
                ConfirmationCode={ConfirmationCode},
                BookingId={BookingId},
                TicketId={TicketId}",
            confirmationCode,
            bookingId,
            ticketId);
    }

    private void LogCancelSuccess(string confirmationCode, long bookingId, long ticketId, int quantity)
    {
        _logger.LogInformation(
            @"Booking cancelled successfully:
                ConfirmationCode={ConfirmationCode},
                BookingId={BookingId},
                TicketId={TicketId},
                Quantity={Quantity}",
            confirmationCode,
            bookingId,
            ticketId,
            quantity);
    }

    private void LogTicketMissingForBooking(long ticketId, string confirmationCode, string operation)
    {
        _logger.LogError(
            "Data inconsistency during {Operation}: Ticket with ID {TicketId} not found for booking {ConfirmationCode}.",
            operation,
            ticketId,
            confirmationCode);
    }

    #endregion
}
