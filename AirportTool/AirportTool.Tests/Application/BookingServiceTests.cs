using AirportTool.Application;
using AirportTool.Domain;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AirportTool.Tests.Application;

public class BookingService_Tests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IBookingRepository> _bookingRepository;
    private readonly Mock<ITicketRepository> _ticketRepository;
    private readonly Mock<IDtoValidator> _dtoValidator;
    private readonly Mock<IUniqueCodeGenerator> _codeGenerator;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ILogger<BookingService>> _logger;

    private readonly BookingService _sut;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public BookingService_Tests()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _bookingRepository = new Mock<IBookingRepository>(MockBehavior.Strict);
        _ticketRepository = new Mock<ITicketRepository>(MockBehavior.Strict);
        _dtoValidator = new Mock<IDtoValidator>(MockBehavior.Strict);
        _codeGenerator = new Mock<IUniqueCodeGenerator>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _logger = new Mock<ILogger<BookingService>>();

        _unitOfWork.SetupGet(x => x.Bookings).Returns(_bookingRepository.Object);
        _unitOfWork.SetupGet(x => x.Tickets).Returns(_ticketRepository.Object);

        _sut = new BookingService(
            _unitOfWork.Object,
            _dtoValidator.Object,
            _codeGenerator.Object,
            _mapper.Object,
            _logger.Object);
    }

    #region GetBookingByCodeAsync Tests

    [Fact]
    public async Task GetBookingByCodeAsync_WhenBookingNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync((Booking?)null);

        // Act
        var result = await _sut.GetBookingByCodeAsync(confirmationCode, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Booking with confirmation code '{confirmationCode}' not found.");
    }

    [Fact]
    public async Task GetBookingByCodeAsync_WhenTicketMissingForBooking_ReturnsUnexpectedFailure()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();

        var booking = _fixture.Build<Booking>()
                              .With(b => b.ConfirmationCode, confirmationCode)
                              .Create();

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync(booking);

        _ticketRepository.Setup(r => r.GetByIdAsync(booking.TicketId, _ct))
                         .ReturnsAsync((Ticket?)null);

        // Act
        var result = await _sut.GetBookingByCodeAsync(confirmationCode, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Unexpected &&
            e.Message == $"Ticket with ID '{booking.TicketId}' not found for booking '{confirmationCode}'.");
    }

    [Fact]
    public async Task GetBookingByCodeAsync_WhenBookingAndTicketExist_ReturnsDtoAndSuccess()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();
        var quantity = _fixture.Create<int>() % 5 + 1;
        var basePrice = _fixture.Create<decimal>() % 500 + 10;
        var taxes = _fixture.Create<decimal>() % 200 + 1;

        var booking = _fixture.Build<Booking>()
                              .With(b => b.ConfirmationCode, confirmationCode)
                              .With(b => b.Quantity, quantity)
                              .Create();

        var ticket = _fixture.Build<Ticket>()
                             .With(t => t.TicketId, booking.TicketId)
                             .With(t => t.BasePrice, basePrice)
                             .With(t => t.Taxes, taxes)
                             .Create();

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync(booking);

        _ticketRepository.Setup(r => r.GetByIdAsync(booking.TicketId, _ct))
                         .ReturnsAsync(ticket);

        var mappedDto = _fixture.Build<GetBookingDto>().Create();
        _mapper.Setup(m => m.Map<GetBookingDto>(booking))
               .Returns(mappedDto);

        // Act
        var result = await _sut.GetBookingByCodeAsync(confirmationCode, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(mappedDto);

        var expectedTotal = (basePrice + taxes) * quantity;
        result.Value!.TotalPrice.Should().Be(expectedTotal);

        _mapper.Verify(m => m.Map<GetBookingDto>(booking), Times.Once);
    }

    #endregion

    #region CreateBookingAsync Tests

    [Fact]
    public async Task CreateBookingAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotSave()
    {
        // Arrange
        var dto = _fixture.Create<CreateBookingDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid DTO" }
        };
        var dtoValidationResult = new Result(errors);

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        // Act
        var result = await _sut.CreateBookingAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(errors);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _bookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _ticketRepository.Verify(r => r.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenTicketNotFound_ReturnsValidationFailureAndDoesNotSave()
    {
        // Arrange
        var dto = _fixture.Create<CreateBookingDto>();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        _ticketRepository.Setup(r => r.GetByIdAsync(dto.TicketId, _ct))
                         .ReturnsAsync((Ticket?)null);

        // Act
        var result = await _sut.CreateBookingAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Ticket with ID '{dto.TicketId}' not found.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _bookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _ticketRepository.Verify(r => r.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenRequestedQuantityExceedsSeatInventory_ReturnsValidationFailureAndDoesNotSave()
    {
        // Arrange
        var seatInventory = 1;
        var requestedQuantity = 2;

        var dto = _fixture.Build<CreateBookingDto>()
                          .With(d => d.Quantity, requestedQuantity)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var ticket = _fixture.Build<Ticket>()
                             .With(t => t.TicketId, dto.TicketId)
                             .With(t => t.SeatInventory, seatInventory)
                             .Create();

        _ticketRepository.Setup(r => r.GetByIdAsync(dto.TicketId, _ct))
                         .ReturnsAsync(ticket);

        // Act
        var result = await _sut.CreateBookingAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Only {seatInventory} seats available, but {requestedQuantity} were requested");

        _bookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _ticketRepository.Verify(r => r.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSaveThrowsConcurrencyException_ReturnsConflictFailure()
    {
        // Arrange
        var quantity = _fixture.Create<int>() % 5 + 1;

        var dto = _fixture.Build<CreateBookingDto>()
                          .With(d => d.Quantity, quantity)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var ticket = _fixture.Build<Ticket>()
                             .With(t => t.TicketId, dto.TicketId)
                             .With(t => t.SeatInventory, quantity + 3)
                             .Create();

        _ticketRepository.Setup(r => r.GetByIdAsync(dto.TicketId, _ct))
                         .ReturnsAsync(ticket);

        var mappedBooking = _fixture.Build<Booking>()
                                    .With(b => b.TicketId, dto.TicketId)
                                    .With(b => b.Quantity, dto.Quantity)
                                    .Create();

        _mapper.Setup(m => m.Map<Booking>(dto))
               .Returns(mappedBooking);

        _codeGenerator.Setup(g => g.Generate())
                      .Returns(_fixture.Create<string>());

        _bookingRepository.Setup(r => r.AddAsync(mappedBooking, _ct))
                          .Returns(Task.CompletedTask);

        _ticketRepository.Setup(r => r.UpdateAsync(ticket, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ThrowsAsync(new DatabaseConcurrencyException("conflict"));

        // Act
        var result = await _sut.CreateBookingAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Conflict &&
            e.Message == "Seat availability changed while processing your request. Please retry.");

        _bookingRepository.Verify(r => r.AddAsync(mappedBooking, _ct), Times.Once);
        _ticketRepository.Verify(r => r.UpdateAsync(ticket, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenCreatedBookingNotRetrievable_ReturnsUnexpectedFailure()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();
        var quantity = _fixture.Create<int>() % 5 + 1;

        var dto = _fixture.Build<CreateBookingDto>()
                          .With(d => d.Quantity, quantity)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var ticket = _fixture.Build<Ticket>()
                             .With(t => t.TicketId, dto.TicketId)
                             .With(t => t.SeatInventory, quantity + 5)
                             .With(t => t.BasePrice, 100m)
                             .With(t => t.Taxes, 20m)
                             .Create();

        _ticketRepository.Setup(r => r.GetByIdAsync(dto.TicketId, _ct))
                         .ReturnsAsync(ticket);

        var mappedBooking = _fixture.Build<Booking>()
                                    .With(b => b.TicketId, dto.TicketId)
                                    .With(b => b.Quantity, dto.Quantity)
                                    .Create();

        _mapper.Setup(m => m.Map<Booking>(dto))
               .Returns(mappedBooking);

        _codeGenerator.Setup(g => g.Generate())
                      .Returns(confirmationCode);

        _bookingRepository.Setup(r => r.AddAsync(mappedBooking, _ct))
                          .Returns(Task.CompletedTask);

        _ticketRepository.Setup(r => r.UpdateAsync(ticket, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync((Booking?)null);

        // Act
        var result = await _sut.CreateBookingAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Unexpected &&
            e.Message == "Booking not found after creation.");

        _bookingRepository.Verify(r => r.AddAsync(mappedBooking, _ct), Times.Once);
        _ticketRepository.Verify(r => r.UpdateAsync(ticket, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenAllValidationsPass_PersistsBookingDecrementsInventoryAndReturnsDto()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();
        var quantity = _fixture.Create<int>() % 5 + 1;
        var basePrice = 150m;
        var taxes = 30m;

        var dto = _fixture.Build<CreateBookingDto>()
                          .With(d => d.Quantity, quantity)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var ticket = _fixture.Build<Ticket>()
                             .With(t => t.TicketId, dto.TicketId)
                             .With(t => t.SeatInventory, quantity + 10)
                             .With(t => t.BasePrice, basePrice)
                             .With(t => t.Taxes, taxes)
                             .Create();

        _ticketRepository.Setup(r => r.GetByIdAsync(dto.TicketId, _ct))
                         .ReturnsAsync(ticket);

        var mappedBooking = _fixture.Build<Booking>()
                                    .With(b => b.TicketId, dto.TicketId)
                                    .With(b => b.Quantity, dto.Quantity)
                                    .Create();

        _mapper.Setup(m => m.Map<Booking>(dto))
               .Returns(mappedBooking);

        _codeGenerator.Setup(g => g.Generate())
                      .Returns(confirmationCode);

        _bookingRepository.Setup(r => r.AddAsync(mappedBooking, _ct))
                          .Returns(Task.CompletedTask);

        _ticketRepository.Setup(r => r.UpdateAsync(ticket, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        var createdBooking = _fixture.Build<Booking>()
                                     .With(b => b.ConfirmationCode, confirmationCode)
                                     .With(b => b.TicketId, dto.TicketId)
                                     .With(b => b.Quantity, dto.Quantity)
                                     .Create();

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync(createdBooking);

        var mappedGetDto = _fixture.Build<GetBookingDto>().Create();
        _mapper.Setup(m => m.Map<GetBookingDto>(createdBooking))
               .Returns(mappedGetDto);

        var seatInventoryBefore = ticket.SeatInventory;

        // Act
        var result = await _sut.CreateBookingAsync(dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(mappedGetDto);

        mappedBooking.ConfirmationCode.Should().Be(confirmationCode);
        mappedBooking.Status.Should().Be(BookingStatus.Active);

        ticket.SeatInventory.Should().Be(seatInventoryBefore - quantity);

        var expectedTotal = (basePrice + taxes) * quantity;
        result.Value!.TotalPrice.Should().Be(expectedTotal);

        _mapper.Verify(m => m.Map<Booking>(dto), Times.Once);
        _mapper.Verify(m => m.Map<GetBookingDto>(createdBooking), Times.Once);
        _bookingRepository.Verify(r => r.AddAsync(mappedBooking, _ct), Times.Once);
        _ticketRepository.Verify(r => r.UpdateAsync(ticket, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
        _bookingRepository.Verify(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct), Times.Once);
    }

    #endregion

    #region CancelBookingAsync Tests

    [Fact]
    public async Task CancelBookingAsync_WhenBookingNotFound_ReturnsNotFoundFailureAndDoesNotSave()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync((Booking?)null);

        // Act
        var result = await _sut.CancelBookingAsync(confirmationCode, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Booking with confirmation code '{confirmationCode}' not found.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _bookingRepository.Verify(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _ticketRepository.Verify(r => r.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenBookingAlreadyCancelled_ReturnsSuccessAndDoesNotSave()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();

        var booking = _fixture.Build<Booking>()
                              .With(b => b.ConfirmationCode, confirmationCode)
                              .With(b => b.Status, BookingStatus.Cancelled)
                              .Create();

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync(booking);

        // Act
        var result = await _sut.CancelBookingAsync(confirmationCode, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        _bookingRepository.Verify(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _ticketRepository.Verify(r => r.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenTicketMissingForBooking_ReturnsUnexpectedFailure()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();

        var booking = _fixture.Build<Booking>()
                              .With(b => b.ConfirmationCode, confirmationCode)
                              .With(b => b.Status, BookingStatus.Active)
                              .Create();

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync(booking);

        _ticketRepository.Setup(r => r.GetByIdAsync(booking.TicketId, _ct))
                         .ReturnsAsync((Ticket?)null);

        // Act
        var result = await _sut.CancelBookingAsync(confirmationCode, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Unexpected &&
            e.Message == $"Ticket with ID '{booking.TicketId}' not found for booking '{confirmationCode}'.");

        _bookingRepository.Verify(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _ticketRepository.Verify(r => r.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenSaveThrowsConcurrencyException_ReturnsConflictFailure()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();
        var quantity = _fixture.Create<int>() % 5 + 1;

        var booking = _fixture.Build<Booking>()
                              .With(b => b.ConfirmationCode, confirmationCode)
                              .With(b => b.Status, BookingStatus.Active)
                              .With(b => b.Quantity, quantity)
                              .Create();

        var ticket = _fixture.Build<Ticket>()
                             .With(t => t.TicketId, booking.TicketId)
                             .With(t => t.SeatInventory, 0)
                             .Create();

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync(booking);

        _ticketRepository.Setup(r => r.GetByIdAsync(booking.TicketId, _ct))
                         .ReturnsAsync(ticket);

        _bookingRepository.Setup(r => r.UpdateAsync(booking, _ct))
                          .Returns(Task.CompletedTask);

        _ticketRepository.Setup(r => r.UpdateAsync(ticket, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ThrowsAsync(new DatabaseConcurrencyException("conflict"));

        // Act
        var result = await _sut.CancelBookingAsync(confirmationCode, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Conflict &&
            e.Message == "Booking or ticket was updated by another request. Please retry.");

        _bookingRepository.Verify(r => r.UpdateAsync(booking, _ct), Times.Once);
        _ticketRepository.Verify(r => r.UpdateAsync(ticket, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenAllValidationsPass_CancelsBookingRestoresInventoryAndSaves()
    {
        // Arrange
        var confirmationCode = _fixture.Create<string>();
        var quantity = _fixture.Create<int>() % 5 + 1;
        var initialInventory = _fixture.Create<int>() % 50;

        var booking = _fixture.Build<Booking>()
                              .With(b => b.ConfirmationCode, confirmationCode)
                              .With(b => b.Status, BookingStatus.Active)
                              .With(b => b.Quantity, quantity)
                              .Create();

        var ticket = _fixture.Build<Ticket>()
                             .With(t => t.TicketId, booking.TicketId)
                             .With(t => t.SeatInventory, initialInventory)
                             .Create();

        _bookingRepository.Setup(r => r.GetByConfirmationCodeAsync(confirmationCode, _ct))
                          .ReturnsAsync(booking);

        _ticketRepository.Setup(r => r.GetByIdAsync(booking.TicketId, _ct))
                         .ReturnsAsync(ticket);

        _bookingRepository.Setup(r => r.UpdateAsync(booking, _ct))
                          .Returns(Task.CompletedTask);

        _ticketRepository.Setup(r => r.UpdateAsync(ticket, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.CancelBookingAsync(confirmationCode, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        booking.Status.Should().Be(BookingStatus.Cancelled);
        ticket.SeatInventory.Should().Be(initialInventory + quantity);

        _bookingRepository.Verify(r => r.UpdateAsync(booking, _ct), Times.Once);
        _ticketRepository.Verify(r => r.UpdateAsync(ticket, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion
}
