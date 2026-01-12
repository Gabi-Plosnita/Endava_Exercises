using AirportTool.Application;
using AirportTool.Domain;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AirportTool.Tests.Application.Services;

public class TicketServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITicketRepository> _ticketRepository;
    private readonly Mock<IFlightScheduleRepository> _flightScheduleRepository;
    private readonly Mock<IDtoValidator> _dtoValidator;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ILogger<TicketService>> _logger;

    private readonly TicketService _sut;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public TicketServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _ticketRepository = new Mock<ITicketRepository>(MockBehavior.Strict);
        _flightScheduleRepository = new Mock<IFlightScheduleRepository>(MockBehavior.Strict);
        _dtoValidator = new Mock<IDtoValidator>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _logger = new Mock<ILogger<TicketService>>();

        _unitOfWork.SetupGet(x => x.Tickets).Returns(_ticketRepository.Object);
        _unitOfWork.SetupGet(x => x.FlightSchedules).Returns(_flightScheduleRepository.Object);

        _sut = new TicketService(
            _unitOfWork.Object,
            _dtoValidator.Object,
            _logger.Object,
            _mapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenTicketNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var ticketId = _fixture.Create<long>();

        _ticketRepository.Setup(r => r.GetDtoByIdAsync(ticketId, _ct))
                         .ReturnsAsync((GetTicketDto?)null);

        // Act
        var result = await _sut.GetByIdAsync(ticketId, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Ticket with ID '{ticketId}' not found.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenTicketFound_ReturnsDtoAndSuccess()
    {
        // Arrange
        var ticketId = _fixture.Create<long>();
        var dto = _fixture.Build<GetTicketDto>().Create();

        _ticketRepository.Setup(r => r.GetDtoByIdAsync(ticketId, _ct))
                         .ReturnsAsync(dto);

        // Act
        var result = await _sut.GetByIdAsync(ticketId, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(dto);
    }

    #endregion

    #region GetByFlightScheduleIdAsync Tests

    [Fact]
    public async Task GetByFlightScheduleIdAsync_WhenFlightScheduleNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var flightScheduleId = _fixture.Create<int>();
        _flightScheduleRepository.Setup(r => r.GetByIdAsync(flightScheduleId, _ct))
                                 .ReturnsAsync((FlightSchedule?)null);

        // Act
        var result = await _sut.GetByFlightScheduleIdAsync(flightScheduleId, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Flight schedule with ID '{flightScheduleId}' does not exist.");

        _ticketRepository.Verify(r => r.GetByFlightScheduleIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),Times.Never);
    }

    [Fact]
    public async Task GetByFlightScheduleIdAsync_WhenFlightScheduleExists_ReturnsTicketsFromRepository()
    {
        // Arrange
        var flightScheduleId = _fixture.Create<int>();

        _flightScheduleRepository.Setup(r => r.GetByIdAsync(flightScheduleId, _ct))
                                 .ReturnsAsync(_fixture.Create<FlightSchedule>());

        var tickets = _fixture.CreateMany<GetTicketDto>(3).ToList().AsReadOnly();
        _ticketRepository.Setup(r => r.GetByFlightScheduleIdAsync(flightScheduleId, _ct))
                         .ReturnsAsync(tickets);

        // Act
        var result = await _sut.GetByFlightScheduleIdAsync(flightScheduleId, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(tickets);

        _ticketRepository.Verify(r => r.GetByFlightScheduleIdAsync(flightScheduleId, _ct), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<CreateTicketDto>();

        var validationErrors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid DTO" }
        };
        var dtoValidationResult = new Result(validationErrors);

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(validationErrors);

        _ticketRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenFlightScheduleDoesNotExist_ReturnsNotFoundFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<CreateTicketDto>();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        _flightScheduleRepository.Setup(r => r.GetByIdAsync(dto.FlightScheduleId, _ct))
                                 .ReturnsAsync((FlightSchedule?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Flight schedule with ID '{dto.FlightScheduleId}' does not exist.");

        _ticketRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenFareClassAlreadyExistsForSchedule_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<CreateTicketDto>();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        _flightScheduleRepository.Setup(r => r.GetByIdAsync(dto.FlightScheduleId, _ct))
                                 .ReturnsAsync(_fixture.Create<FlightSchedule>());

        _ticketRepository.Setup(r => r.FareClassExistsForScheduleAsync(dto.FlightScheduleId, dto.FareClass, null, _ct))
                         .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Fare class {dto.FareClass} already exists for flight schedule ID {dto.FlightScheduleId}.");

        _ticketRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCreatedTicketNotRetrievable_ReturnsUnexpectedFailure()
    {
        // Arrange
        var dto = _fixture.Create<CreateTicketDto>();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        _flightScheduleRepository.Setup(r => r.GetByIdAsync(dto.FlightScheduleId, _ct))
                                 .ReturnsAsync(_fixture.Create<FlightSchedule>());

        _ticketRepository.Setup(r => r.FareClassExistsForScheduleAsync(dto.FlightScheduleId, dto.FareClass, null, _ct))
                         .ReturnsAsync(false);

        var mappedTicket = _fixture.Build<Ticket>()
                                   .With(t => t.FlightScheduleId, dto.FlightScheduleId)
                                   .With(t => t.FareClass, dto.FareClass)
                                   .Create();

        _mapper.Setup(m => m.Map<Ticket>(dto))
               .Returns(mappedTicket);

        var generatedId = _fixture.Create<long>();

        _ticketRepository.Setup(r => r.AddAndSaveAsync(mappedTicket, _ct))
                         .Returns(Task.CompletedTask)
                         .Callback(() => mappedTicket.TicketId = generatedId);

        _ticketRepository.Setup(r => r.GetDtoByIdAsync(generatedId, _ct))
                         .ReturnsAsync((GetTicketDto?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Unexpected &&
            e.Message == "Ticket not found after creation.");

        _ticketRepository.Verify(r => r.AddAndSaveAsync(mappedTicket, _ct), Times.Once);
        _ticketRepository.Verify(r => r.GetDtoByIdAsync(generatedId, _ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAllValidationsPass_PersistsTicketAndReturnsCreatedDto()
    {
        // Arrange
        var dto = _fixture.Create<CreateTicketDto>();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        _flightScheduleRepository.Setup(r => r.GetByIdAsync(dto.FlightScheduleId, _ct))
                                 .ReturnsAsync(_fixture.Create<FlightSchedule>());

        _ticketRepository.Setup(r => r.FareClassExistsForScheduleAsync(dto.FlightScheduleId, dto.FareClass, null, _ct))
                         .ReturnsAsync(false);

        var mappedTicket = _fixture.Build<Ticket>()
                                   .With(t => t.FlightScheduleId, dto.FlightScheduleId)
                                   .With(t => t.FareClass, dto.FareClass)
                                   .Create();

        _mapper.Setup(m => m.Map<Ticket>(dto))
               .Returns(mappedTicket);

        var generatedId = _fixture.Create<long>();

        _ticketRepository.Setup(r => r.AddAndSaveAsync(mappedTicket, _ct))
                         .Returns(Task.CompletedTask)
                         .Callback(() => mappedTicket.TicketId = generatedId);

        var createdDto = _fixture.Create<GetTicketDto>();

        _ticketRepository.Setup(r => r.GetDtoByIdAsync(generatedId, _ct))
                         .ReturnsAsync(createdDto);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(createdDto);

        _mapper.Verify(m => m.Map<Ticket>(dto), Times.Once);
        _ticketRepository.Verify(r => r.AddAndSaveAsync(mappedTicket, _ct), Times.Once);
        _ticketRepository.Verify(r => r.GetDtoByIdAsync(generatedId, _ct), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotSave()
    {
        // Arrange
        var ticketId = _fixture.Create<long>();
        var dto = _fixture.Create<UpdateTicketDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid DTO" }
        };
        var dtoValidationResult = new Result(errors);

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        // Act
        var result = await _sut.UpdateAsync(ticketId, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(errors);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _ticketRepository.Verify(r => r.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenTicketNotFound_ReturnsNotFoundFailureAndDoesNotSave()
    {
        // Arrange
        var ticketId = _fixture.Create<long>();
        var dto = _fixture.Create<UpdateTicketDto>();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        _ticketRepository.Setup(r => r.GetByIdAsync(ticketId, _ct))
                         .ReturnsAsync((Ticket?)null);

        // Act
        var result = await _sut.UpdateAsync(ticketId, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Ticket with ID '{ticketId}' does not exist.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _ticketRepository.Verify(r => r.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAllValidationsPass_UpdatesTicketAndSavesChanges()
    {
        // Arrange
        var ticketId = _fixture.Create<long>();
        var dto = _fixture.Create<UpdateTicketDto>();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var existingTicket = _fixture.Build<Ticket>()
                                     .With(t => t.TicketId, ticketId)
                                     .Create();

        _ticketRepository.Setup(r => r.GetByIdAsync(ticketId, _ct))
                         .ReturnsAsync(existingTicket);

        _mapper.Setup(m => m.Map(dto, existingTicket))
               .Returns(existingTicket);

        _ticketRepository.Setup(r => r.UpdateAsync(existingTicket, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(ticketId, dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mapper.Verify(m => m.Map(dto, existingTicket), Times.Once);
        _ticketRepository.Verify(r => r.UpdateAsync(existingTicket, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion

    #region DeleteByIdAsync Tests

    [Fact]
    public async Task DeleteByIdAsync_WhenTicketNotFound_ReturnsNotFoundFailureAndDoesNotRemove()
    {
        // Arrange
        var ticketId = _fixture.Create<long>();

        _ticketRepository.Setup(r => r.GetByIdAsync(ticketId, _ct))
                         .ReturnsAsync((Ticket?)null);

        // Act
        var result = await _sut.DeleteByIdAsync(ticketId, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Ticket with ID '{ticketId}' does not exist.");

        _ticketRepository.Verify(r => r.RemoveAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteByIdAsync_WhenTicketHasBookings_ReturnsValidationFailureAndDoesNotRemove()
    {
        // Arrange
        var ticketId = _fixture.Create<long>();
        var existingTicket = _fixture.Build<Ticket>()
                                     .With(t => t.TicketId, ticketId)
                                     .Create();

        _ticketRepository.Setup(r => r.GetByIdAsync(ticketId, _ct))
                         .ReturnsAsync(existingTicket);

        _ticketRepository.Setup(r => r.HasBookingsAsync(ticketId, _ct))
                         .ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteByIdAsync(ticketId, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Ticket with ID '{ticketId}' has associated bookings and cannot be deleted.");

        _ticketRepository.Verify(r => r.RemoveAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteByIdAsync_WhenAllValidationsPass_RemovesTicketAndSavesChanges()
    {
        // Arrange
        var ticketId = _fixture.Create<long>();

        var existingTicket = _fixture.Build<Ticket>()
                                     .With(t => t.TicketId, ticketId)
                                     .Create();

        _ticketRepository.Setup(r => r.GetByIdAsync(ticketId, _ct))
                         .ReturnsAsync(existingTicket);

        _ticketRepository.Setup(r => r.HasBookingsAsync(ticketId, _ct))
                         .ReturnsAsync(false);

        _ticketRepository.Setup(r => r.RemoveAsync(existingTicket, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteByIdAsync(ticketId, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        _ticketRepository.Verify(r => r.RemoveAsync(existingTicket, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion
}
