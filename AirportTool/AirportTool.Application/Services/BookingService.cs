
using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Xml.XPath;

namespace AirportTool.Application;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateBookingDto> _createBookingDtoValidator;
    private readonly IUniqueCodeGenerator _codeGenerator;
    private readonly IMapper _mapper;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IUnitOfWork unitOfWork,
        IValidator<CreateBookingDto> createBookingDtoValidator,
        IUniqueCodeGenerator codeGenerator,
        IMapper mapper,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork;
        _createBookingDtoValidator = createBookingDtoValidator;
        _codeGenerator = codeGenerator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<GetBookingDto?>> GetBookingByCodeAsync(string confirmationCode, CancellationToken cancellationToken)
    {
        var result = new Result<GetBookingDto?>();

        var booking = await ValidateBookingExistsAsync(confirmationCode, result, cancellationToken);
        if (result.IsFailure || booking == null)
        {
            return result;
        }

        var ticket = await ValidateTicketExistsAsync(booking.TicketId, result, cancellationToken);
        if (result.IsFailure || ticket == null)
        {
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

        var dtoValidationResult = _createBookingDtoValidator.Validate(createBookingDto);
        result.AddErrors(dtoValidationResult.Errors);
        if (result.IsFailure)
        {
            return result;
        }

        var ticket = await ValidateTicketExistsAsync(createBookingDto.TicketId, result, cancellationToken);
        if (result.IsFailure || ticket == null)
        {
            return result;
        }
        
        ValidateSeatAvailability(createBookingDto.Quantity, ticket.SeatInventory, result);

        if(result.IsFailure)
        {
            return result;
        }

        var booking = _mapper.Map<Booking>(createBookingDto);
        booking.ConfirmationCode = _codeGenerator.Generate();
        ticket.SeatInventory -= createBookingDto.Quantity;
        await _unitOfWork.Bookings.AddAsync(booking, cancellationToken);
        await _unitOfWork.Tickets.UpdateAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdBooking = await _unitOfWork.Bookings.GetByConfirmationCodeAsync(booking.ConfirmationCode, cancellationToken);
        var getBookingDto = _mapper.Map<GetBookingDto>(createdBooking);
        getBookingDto.TotalPrice = CalculateTotalPrice(ticket.BasePrice, ticket.Taxes, booking.Quantity);
        result.Value = getBookingDto;

        return result;
    }

    public async Task<Result> CancelBookingAsync(string confirmationCode, CancellationToken cancellationToken)
    {
        var result = new Result();

        var booking = await ValidateBookingExistsAsync(confirmationCode, result, cancellationToken);
        if (result.IsFailure || booking == null)
        {
            return result;
        }

        var ticket = await ValidateTicketExistsAsync(booking.TicketId, result, cancellationToken);
        if (result.IsFailure || ticket == null)
        {
            return result;
        }

        booking.Status = BookingStatus.Cancelled;
        ticket.SeatInventory += booking.Quantity;
        await _unitOfWork.Bookings.UpdateAsync(booking, cancellationToken);
        await _unitOfWork.Tickets.UpdateAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
                Type = ErrorType.NotFound
            };
            result.AddError(error); 
        }
        return ticket;
    }

    private void ValidateSeatAvailability(int quantity, int seatInventory, Result result)
    {
        if(quantity > seatInventory)
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

    #endregion
}
