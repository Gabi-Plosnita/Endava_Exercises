
using AirportTool.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;

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
        getBookingDto.TotalPrice = ticket.BasePrice * booking.Quantity;
        result.Value = getBookingDto;

        return result;
    }

    public Task<Result<GetBookingDto?>> CreateBookingAsync(CreateBookingDto createBookingDto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> CancelBookingAsync(string confirmationCode, CancellationToken cancellationToken)
    {
        var result = new Result();

        var getBookingDto = await ValidateBookingExistsAsync(confirmationCode, result, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }


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
    #endregion
}
