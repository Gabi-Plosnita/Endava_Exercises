
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

    public async Task<GetBookingDto?> GetBookingByCodeAsync(string confirmationCode, CancellationToken cancellationToken)
    {
        var booking = await _unitOfWork.Bookings.GetDtoByConfirmationCodeAsync(confirmationCode, cancellationToken);
        return booking;
    }

    public Task<Result<GetBookingDto?>> CreateBookingAsync(CreateBookingDto createBookingDto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result> CancelBookingAsync(string confirmationCode, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
