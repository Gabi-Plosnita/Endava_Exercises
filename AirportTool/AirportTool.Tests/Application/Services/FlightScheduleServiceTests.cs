using AirportTool.Application;
using AirportTool.Domain;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AirportTool.Tests.Application.Services;

public class FlightSchedulesServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IFlightScheduleRepository> _flightScheduleRepository;
    private readonly Mock<IFlightRepository> _flightRepository;
    private readonly Mock<IGateRepository> _gateRepository;
    private readonly Mock<IAircraftRepository> _aircraftRepository;
    private readonly Mock<IDtoValidator> _dtoValidator;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ILogger<FlightSchedulesService>> _logger;

    private readonly FlightSchedulesService _sut;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture;

    public FlightSchedulesServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _flightScheduleRepository = new Mock<IFlightScheduleRepository>(MockBehavior.Strict);
        _flightRepository = new Mock<IFlightRepository>(MockBehavior.Strict);
        _gateRepository = new Mock<IGateRepository>(MockBehavior.Strict);
        _aircraftRepository = new Mock<IAircraftRepository>(MockBehavior.Strict);
        _dtoValidator = new Mock<IDtoValidator>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _logger = new Mock<ILogger<FlightSchedulesService>>();
        _fixture = new Fixture();
        _fixture.Register(() => DateOnly.FromDateTime(_fixture.Create<DateTime>()));

        _unitOfWork.SetupGet(x => x.FlightSchedules).Returns(_flightScheduleRepository.Object);
        _unitOfWork.SetupGet(x => x.Flights).Returns(_flightRepository.Object);
        _unitOfWork.SetupGet(x => x.Gates).Returns(_gateRepository.Object);
        _unitOfWork.SetupGet(x => x.Aircrafts).Returns(_aircraftRepository.Object);

        _sut = new FlightSchedulesService(
            _unitOfWork.Object,
            _dtoValidator.Object,
            _mapper.Object,
            _logger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenFlightScheduleNotFound_ReturnsNotFoundErrorAndFailure()
    {
        // Arrange
        var id = _fixture.Create<int>();

        _flightScheduleRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
                                 .ReturnsAsync((GetFlightScheduleDto?)null);

        // Act
        var result = await _sut.GetByIdAsync(id, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"FlightSchedule with ID '{id}' not found.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenFlightScheduleFound_ReturnsDtoAndSuccess()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<GetFlightScheduleDto>()
                          .Create();

        _flightScheduleRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
                                 .ReturnsAsync(dto);

        // Act
        var result = await _sut.GetByIdAsync(id, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(dto);
    }

    #endregion

    #region GetByFilterAsync Tests

    [Fact]
    public async Task GetByFilterAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotCallRepository()
    {
        // Arrange
        var dto = _fixture.Create<FlightScheduleFilterDto>();

        var validationErrors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid filter" }
        };
        var dtoValidationResult = new Result(validationErrors);

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        // Act
        var result = await _sut.GetByFilterAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(validationErrors);

        _flightScheduleRepository.Verify(
            r => r.GetFilteredFlightSchedulesAsync(It.IsAny<FlightScheduleFilterDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByFilterAsync_WhenDtoIsValid_ReturnsPagedResultFromRepository()
    {
        // Arrange
        var dto = _fixture.Create<FlightScheduleFilterDto>();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var paged = _fixture.Build<PagedResult<FlightScheduleSearchDto>>()
                            .With(p => p.PageIndex, dto.PageIndex)
                            .With(p => p.PageSize, dto.PageSize)
                            .With(p => p.Items, _fixture.CreateMany<FlightScheduleSearchDto>(3).ToList())
                            .Create();

        _flightScheduleRepository.Setup(r => r.GetFilteredFlightSchedulesAsync(dto, _ct))
                                 .ReturnsAsync(paged);

        // Act
        var result = await _sut.GetByFilterAsync(dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(paged);

        _flightScheduleRepository.Verify(r => r.GetFilteredFlightSchedulesAsync(dto, _ct), Times.Once);
    }

    #endregion

    #region GetDailyStatsAsync Tests

    [Fact]
    public async Task GetDailyStatsAsync_WhenStartIsGreaterOrEqualThanEnd_ReturnsValidationFailureAndDoesNotCallRepository()
    {
        // Arrange
        var day = _fixture.Create<DateOnly>();
        var startUtc = day;
        var endUtc = day; 

        // Act
        var result = await _sut.GetDailyStatsAsync(startUtc, endUtc, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();

        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == "The startUtc must be earlier than endUtc.");

        _flightScheduleRepository.Verify(
            r => r.GetDailyStatsAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDailyStatsAsync_WhenDateRangeIsValid_ReturnsStatsFromRepository()
    {
        // Arrange
        var startUtc = new DateOnly(2025, 1, 1);
        var endUtc = new DateOnly(2025, 1, 2);

        var stats = _fixture.CreateMany<DailyFlightStatsDto>(2).ToList();

        _flightScheduleRepository.Setup(r => r.GetDailyStatsAsync(startUtc, endUtc, _ct))
                                 .ReturnsAsync(stats);

        // Act
        var result = await _sut.GetDailyStatsAsync(startUtc, endUtc, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(stats);

        _flightScheduleRepository.Verify(r => r.GetDailyStatsAsync(startUtc, endUtc, _ct), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<UpsertFlightScheduleDto>();

        var validationErrors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid DTO" }
        };
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result(validationErrors));

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FlightSchedule.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(validationErrors);

        _flightScheduleRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<FlightSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenFlightDoesNotExist_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, (string?)null)
                          .With(d => d.AssignedAircraftTail, (string?)null)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        _flightRepository.Setup(r => r.GetByIdAsync(dto.FlightId, _ct))
                         .ReturnsAsync((Flight?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FlightSchedule.Should().BeNull();

        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Flight with ID '{dto.FlightId}' does not exist.");

        _flightScheduleRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<FlightSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenGateCodeProvidedButGateNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var gateCode = _fixture.Create<string>();
        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, gateCode)
                          .With(d => d.AssignedAircraftTail, (string?)null)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var flight = _fixture.Build<Flight>()
                             .With(f => f.FlightId, dto.FlightId)
                             .Create();

        _flightRepository.Setup(r => r.GetByIdAsync(dto.FlightId, _ct))
                         .ReturnsAsync(flight);

        _gateRepository.Setup(r => r.GetByCodeAndAirportAsync(gateCode, flight.OriginAirportId, _ct))
                       .ReturnsAsync((Gate?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Gate with Code '{gateCode}' not found for airport '{flight.OriginAirportId}'");

        _flightScheduleRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<FlightSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenAssignedAircraftTailProvidedButAircraftNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var assignedTail = _fixture.Create<string>();
        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, (string?)null)
                          .With(d => d.AssignedAircraftTail, assignedTail)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var flight = _fixture.Build<Flight>()
                             .With(f => f.FlightId, dto.FlightId)
                             .Create();

        _flightRepository.Setup(r => r.GetByIdAsync(dto.FlightId, _ct))
                         .ReturnsAsync(flight);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(assignedTail, _ct))
                           .ReturnsAsync((Aircraft?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Aircraft with Tail '{assignedTail}' not found");

        _flightScheduleRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<FlightSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenGateOverlapsExist_ReturnsFailureAndReturnsConflicts()
    {
        // Arrange
        var gateCode = _fixture.Create<string>();

        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, gateCode)
                          .With(d => d.AssignedAircraftTail, (string?)null)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var flight = _fixture.Build<Flight>()
                             .With(f => f.FlightId, dto.FlightId)
                             .Create();

        _flightRepository.Setup(r => r.GetByIdAsync(dto.FlightId, _ct))
                         .ReturnsAsync(flight);

        var gate = _fixture.Build<Gate>()
                           .With(g => g.Code, gateCode)
                           .Create();

        _gateRepository.Setup(r => r.GetByCodeAndAirportAsync(gateCode, flight.OriginAirportId, _ct))
                       .ReturnsAsync(gate);

        var conflicts = _fixture.CreateMany<ScheduleConflictDto>(2).ToList();

        _flightScheduleRepository.Setup(r => r.GetGateOverlapsAsync(gate.GateId, dto.ScheduledDepartureUtc, dto.ScheduledArrivalUtc, null, _ct))
                                 .ReturnsAsync(conflicts);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ScheduleConflicts.Should().BeEquivalentTo(conflicts);

        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Gate overlap at gate with code '{gateCode}'. {conflicts.Count} schedule conflict(s) found in the selected time window.");

        _flightScheduleRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<FlightSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCreatedScheduleNotRetrievable_ReturnsUnexpectedFailure()
    {
        // Arrange
        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, (string?)null)
                          .With(d => d.AssignedAircraftTail, (string?)null)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var flight = _fixture.Build<Flight>()
                             .With(f => f.FlightId, dto.FlightId)
                             .Create();

        _flightRepository.Setup(r => r.GetByIdAsync(dto.FlightId, _ct))
                         .ReturnsAsync(flight);

        var mapped = new FlightSchedule();
        _mapper.Setup(m => m.Map<FlightSchedule>(dto))
               .Returns(mapped);

        var generatedId = _fixture.Create<int>();
        _flightScheduleRepository.Setup(r => r.AddAndSaveAsync(mapped, _ct))
                                 .Returns(Task.CompletedTask)
                                 .Callback(() => mapped.FlightScheduleId = generatedId);

        _flightScheduleRepository.Setup(r => r.GetDtoByIdAsync(generatedId, _ct))
                                 .ReturnsAsync((GetFlightScheduleDto?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Unexpected &&
            e.Message == $"FlightSchedule with ID '{generatedId}' does not exist.");

        _mapper.Verify(m => m.Map<FlightSchedule>(dto), Times.Once);
        _flightScheduleRepository.Verify(r => r.AddAndSaveAsync(mapped, _ct), Times.Once);
        _flightScheduleRepository.Verify(r => r.GetDtoByIdAsync(generatedId, _ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAllValidationsPass_PersistsScheduleAndReturnsCreatedDto()
    {
        // Arrange
        var gateCode = _fixture.Create<string>();
        var assignedTail = _fixture.Create<string>();

        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, gateCode)
                          .With(d => d.AssignedAircraftTail, assignedTail)
                          .Create();

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result());

        var flight = _fixture.Build<Flight>()
                             .With(f => f.FlightId, dto.FlightId)
                             .Create();

        _flightRepository.Setup(r => r.GetByIdAsync(dto.FlightId, _ct))
                         .ReturnsAsync(flight);

        var gate = _fixture.Build<Gate>()
                           .With(g => g.Code, gateCode)
                           .Create();

        _gateRepository.Setup(r => r.GetByCodeAndAirportAsync(gateCode, flight.OriginAirportId, _ct))
                       .ReturnsAsync(gate);

        var aircraft = _fixture.Build<Aircraft>()
                               .With(a => a.TailNumber, assignedTail)
                               .Create();

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(assignedTail, _ct))
                           .ReturnsAsync(aircraft);

        _flightScheduleRepository.Setup(r => r.GetGateOverlapsAsync(gate.GateId, dto.ScheduledDepartureUtc, dto.ScheduledArrivalUtc, null, _ct))
                                 .ReturnsAsync(Array.Empty<ScheduleConflictDto>());

        var mapped = new FlightSchedule();
        _mapper.Setup(m => m.Map<FlightSchedule>(dto))
               .Returns(mapped);

        var generatedId = _fixture.Create<int>();
        _flightScheduleRepository.Setup(r => r.AddAndSaveAsync(mapped, _ct))
                                 .Returns(Task.CompletedTask)
                                 .Callback(() => mapped.FlightScheduleId = generatedId);

        var createdDto = _fixture.Create<GetFlightScheduleDto>();
        _flightScheduleRepository.Setup(r => r.GetDtoByIdAsync(generatedId, _ct))
                                 .ReturnsAsync(createdDto);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FlightSchedule.Should().BeSameAs(createdDto);
        result.Value!.ScheduleConflicts.Should().NotBeNull();
        result.Value!.ScheduleConflicts.Should().BeEmpty();

        mapped.Status.Should().Be(FlightScheduleStatus.Planned);
        mapped.GateId.Should().Be(gate.GateId);
        mapped.AssignedAircraftId.Should().Be(aircraft.AircraftId);

        _mapper.Verify(m => m.Map<FlightSchedule>(dto), Times.Once);
        _flightScheduleRepository.Verify(r => r.AddAndSaveAsync(mapped, _ct), Times.Once);
        _flightScheduleRepository.Verify(r => r.GetDtoByIdAsync(generatedId, _ct), Times.Once);
    }

    #endregion

    #region ImportAsync Tests

    [Fact]
    public async Task ImportAsync_WhenScheduleDoesNotExistAndCreateSucceeds_IncrementsCreated()
    {
        // Arrange
        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, (string?)null)
                          .With(d => d.AssignedAircraftTail, (string?)null)
                          .Create();

        var rows = new[] { dto };

        _flightScheduleRepository.Setup(r => r.GetByFlightAndDepartureAsync(dto.FlightId, dto.ScheduledDepartureUtc, _ct))
                                 .ReturnsAsync((FlightSchedule?)null);

        _dtoValidator.Setup(v => v.Validate(dto)).Returns(new Result());

        var flight = _fixture.Build<Flight>()
                             .With(f => f.FlightId, dto.FlightId)
                             .Create();
        _flightRepository.Setup(r => r.GetByIdAsync(dto.FlightId, _ct)).ReturnsAsync(flight);

        var mapped = new FlightSchedule();
        _mapper.Setup(m => m.Map<FlightSchedule>(dto)).Returns(mapped);

        var createdId = _fixture.Create<int>();
        _flightScheduleRepository.Setup(r => r.AddAndSaveAsync(mapped, _ct))
                                 .Returns(Task.CompletedTask)
                                 .Callback(() => mapped.FlightScheduleId = createdId);

        var createdDto = _fixture.Create<GetFlightScheduleDto>();
        _flightScheduleRepository.Setup(r => r.GetDtoByIdAsync(createdId, _ct))
                                 .ReturnsAsync(createdDto);

        // Act
        var summary = await _sut.ImportAsync(rows, _ct);

        // Assert
        summary.Total.Should().Be(1);
        summary.Created.Should().Be(1);
        summary.Updated.Should().Be(0);
        summary.Failed.Should().Be(0);
        summary.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_WhenScheduleDoesNotExistAndCreateReturnsConflicts_IncrementsFailedAndAddsError()
    {
        // Arrange
        var gateCode = _fixture.Create<string>();

        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, gateCode)
                          .With(d => d.AssignedAircraftTail, (string?)null)
                          .Create();

        var rows = new[] { dto };

        _flightScheduleRepository.Setup(r => r.GetByFlightAndDepartureAsync(dto.FlightId, dto.ScheduledDepartureUtc, _ct))
                                 .ReturnsAsync((FlightSchedule?)null);

        _dtoValidator.Setup(v => v.Validate(dto)).Returns(new Result());

        var flight = _fixture.Build<Flight>()
                             .With(f => f.FlightId, dto.FlightId)
                             .Create();
        _flightRepository.Setup(r => r.GetByIdAsync(dto.FlightId, _ct)).ReturnsAsync(flight);

        var gate = _fixture.Build<Gate>()
                           .With(g => g.Code, gateCode)
                           .Create();
        _gateRepository.Setup(r => r.GetByCodeAndAirportAsync(gateCode, flight.OriginAirportId, _ct)).ReturnsAsync(gate);

        var conflicts = _fixture.CreateMany<ScheduleConflictDto>(1).ToList();
        _flightScheduleRepository.Setup(r => r.GetGateOverlapsAsync(gate.GateId, dto.ScheduledDepartureUtc, dto.ScheduledArrivalUtc, null, _ct))
                                 .ReturnsAsync(conflicts);

        // Act
        var summary = await _sut.ImportAsync(rows, _ct);

        // Assert
        summary.Total.Should().Be(1);
        summary.Created.Should().Be(0);
        summary.Updated.Should().Be(0);
        summary.Failed.Should().Be(1);
        summary.Errors.Should().ContainSingle();

        summary.Errors[0].Row.Should().Be(1);
        summary.Errors[0].Message.Should().Contain("Gate overlap");
        summary.Errors[0].Message.Should().Contain(gateCode);
    }

    [Fact]
    public async Task ImportAsync_WhenScheduleExistsAndUpdateFails_IncrementsFailedAndAddsError()
    {
        // Arrange
        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, (string?)null)
                          .With(d => d.AssignedAircraftTail, (string?)null)
                          .Create();

        var rows = new[] { dto };

        var existing = _fixture.Build<FlightSchedule>()
                               .With(s => s.FlightId, dto.FlightId)
                               .With(s => s.ScheduledDepartureUtc, dto.ScheduledDepartureUtc)
                               .Create();

        _flightScheduleRepository.Setup(r => r.GetByFlightAndDepartureAsync(dto.FlightId, dto.ScheduledDepartureUtc, _ct))
                                 .ReturnsAsync(existing);

        var updateErrors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid row DTO" }
        };
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(new Result(updateErrors));

        // Act
        var summary = await _sut.ImportAsync(rows, _ct);

        // Assert
        summary.Total.Should().Be(1);
        summary.Created.Should().Be(0);
        summary.Updated.Should().Be(0);
        summary.Failed.Should().Be(1);
        summary.Errors.Should().ContainSingle();

        summary.Errors[0].Row.Should().Be(1);
        summary.Errors[0].Message.Should().Be("Invalid row DTO");

        _flightScheduleRepository.Verify(r => r.UpdateAsync(It.IsAny<FlightSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_WhenScheduleExistsAndUpdateSucceeds_IncrementsUpdated()
    {
        // Arrange
        var dto = _fixture.Build<UpsertFlightScheduleDto>()
                          .With(d => d.GateCode, (string?)null)
                          .With(d => d.AssignedAircraftTail, (string?)null)
                          .Create();

        var rows = new[] { dto };

        var existing = _fixture.Build<FlightSchedule>()
                               .With(s => s.FlightScheduleId, _fixture.Create<int>())
                               .With(s => s.FlightId, dto.FlightId)
                               .With(s => s.ScheduledDepartureUtc, dto.ScheduledDepartureUtc)
                               .Create();

        _flightScheduleRepository.Setup(r => r.GetByFlightAndDepartureAsync(dto.FlightId, dto.ScheduledDepartureUtc, _ct))
                                 .ReturnsAsync(existing);

        _dtoValidator.Setup(v => v.Validate(dto)).Returns(new Result());

        var flight = _fixture.Build<Flight>()
                             .With(f => f.FlightId, dto.FlightId)
                             .Create();
        _flightRepository.Setup(r => r.GetByIdAsync(dto.FlightId, _ct)).ReturnsAsync(flight);

        _flightScheduleRepository.Setup(r => r.UpdateAsync(existing, _ct)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct)).ReturnsAsync(1);

        // Act
        var summary = await _sut.ImportAsync(rows, _ct);

        // Assert
        summary.Total.Should().Be(1);
        summary.Created.Should().Be(0);
        summary.Updated.Should().Be(1);
        summary.Failed.Should().Be(0);
        summary.Errors.Should().BeEmpty();

        _flightScheduleRepository.Verify(r => r.UpdateAsync(existing, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion
}
