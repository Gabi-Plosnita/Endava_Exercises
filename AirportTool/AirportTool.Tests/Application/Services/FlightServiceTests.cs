using AirportTool.Application;
using AirportTool.Domain;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AirportTool.Tests.Application.Services;

public class FlightServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IFlightRepository> _flightRepository;
    private readonly Mock<IAirlineRepository> _airlineRepository;
    private readonly Mock<IAirportRepository> _airportRepository;
    private readonly Mock<IAircraftRepository> _aircraftRepository;
    private readonly Mock<IDtoValidator> _dtoValidator;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ILogger<FlightService>> _logger;

    private readonly FlightService _sut;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public FlightServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _flightRepository = new Mock<IFlightRepository>(MockBehavior.Strict);
        _airlineRepository = new Mock<IAirlineRepository>(MockBehavior.Strict);
        _airportRepository = new Mock<IAirportRepository>(MockBehavior.Strict);
        _aircraftRepository = new Mock<IAircraftRepository>(MockBehavior.Strict);
        _dtoValidator = new Mock<IDtoValidator>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _logger = new Mock<ILogger<FlightService>>();

        _unitOfWork.SetupGet(x => x.Flights).Returns(_flightRepository.Object);
        _unitOfWork.SetupGet(x => x.Airlines).Returns(_airlineRepository.Object);
        _unitOfWork.SetupGet(x => x.Airports).Returns(_airportRepository.Object);
        _unitOfWork.SetupGet(x => x.Aircrafts).Returns(_aircraftRepository.Object);

        _sut = new FlightService(
            _unitOfWork.Object,
            _dtoValidator.Object,
            _mapper.Object,
            _logger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenFlightNotFound_ReturnsNotFoundErrorAndFailure()
    {
        // Arrange
        var flightId = _fixture.Create<int>();

        _flightRepository.Setup(r => r.GetDtoByIdAsync(flightId, _ct))
                         .ReturnsAsync((GetFlightDto?)null);

        // Act
        var result = await _sut.GetByIdAsync(flightId, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Flight with ID {flightId} not found.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenFlightFound_ReturnsDtoAndSuccess()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Build<GetFlightDto>()
                          .With(d => d.FlightId, flightId)
                          .Create();

        _flightRepository.Setup(r => r.GetDtoByIdAsync(flightId, _ct))
                         .ReturnsAsync(dto);

        // Act
        var result = await _sut.GetByIdAsync(flightId, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(dto);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<CreateFlightDto>();

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

        _flightRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenAirlineNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Build<CreateFlightDto>()
                          .With(d => d.DefaultAircraftTail, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync((Airline?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(It.IsAny<string>(), _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airline with IataCode '{dto.AirlineIataCode}' not found.");

        _flightRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenFlightNumberAlreadyExistsForAirline_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Build<CreateFlightDto>()
                          .With(d => d.DefaultAircraftTail, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync(_fixture.Create<Flight>());

        _airportRepository.Setup(r => r.GetByIataCodeAsync(It.IsAny<string>(), _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Flight with Number '{dto.FlightNumber}' already exists for Airline '{airline.Iatacode}'");

        _flightRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenOriginAirportNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Build<CreateFlightDto>()
                          .With(d => d.DefaultAircraftTail, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync((Flight?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync((Airport?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airport with IataCode '{dto.OriginAirportIataCode}' not found.");

        _flightRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenDestinationAirportNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Build<CreateFlightDto>()
                          .With(d => d.DefaultAircraftTail, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync((Flight?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync((Airport?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airport with IataCode '{dto.DestinationAirportIataCode}' not found.");

        _flightRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenDefaultAircraftTailProvidedButAircraftNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<CreateFlightDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var airline = _fixture.Create<Airline>();

        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync((Flight?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.DefaultAircraftTail!, _ct))
                           .ReturnsAsync((Aircraft?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Aircraft with TailNumber '{dto.DefaultAircraftTail}' not found.");

        _flightRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCreatedFlightNotRetrievable_ReturnsUnexpectedFailure()
    {
        // Arrange
        var dto = _fixture.Create<CreateFlightDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync((Flight?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.DefaultAircraftTail!, _ct))
                           .ReturnsAsync(_fixture.Create<Aircraft>());

        var mappedFlight = new Flight { FlightNumber = dto.FlightNumber, IsActive = dto.IsActive };
        _mapper.Setup(m => m.Map<Flight>(dto))
               .Returns(mappedFlight);

        var generatedId = _fixture.Create<int>();
        _flightRepository.Setup(r => r.AddAndSaveAsync(mappedFlight, _ct))
                         .Returns(Task.CompletedTask)
                         .Callback(() => mappedFlight.FlightId = generatedId);

        _flightRepository.Setup(r => r.GetDtoByIdAsync(generatedId, _ct))
                         .ReturnsAsync((GetFlightDto?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Unexpected &&
            e.Message == "Flight not found after creation.");

        _flightRepository.Verify(r => r.AddAndSaveAsync(mappedFlight, _ct), Times.Once);
        _flightRepository.Verify(r => r.GetDtoByIdAsync(generatedId, _ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAllValidationsPass_PersistsFlightAndReturnsCreatedDto()
    {
        // Arrange
        var dto = _fixture.Create<CreateFlightDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync((Flight?)null);

        var originAirport = _fixture.Create<Airport>();
        var destinationAirport = _fixture.Create<Airport>();
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(originAirport);
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(destinationAirport);

        var defaultAircraft = _fixture.Create<Aircraft>();
        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.DefaultAircraftTail!, _ct))
                           .ReturnsAsync(defaultAircraft);

        var mappedFlight = new Flight { FlightNumber = dto.FlightNumber, IsActive = dto.IsActive };
        _mapper.Setup(m => m.Map<Flight>(dto))
               .Returns(mappedFlight);

        var generatedId = _fixture.Create<int>();
        _flightRepository.Setup(r => r.AddAndSaveAsync(mappedFlight, _ct))
                         .Returns(Task.CompletedTask)
                         .Callback(() => mappedFlight.FlightId = generatedId);

        var createdDto = _fixture.Build<GetFlightDto>()
                                 .With(d => d.FlightId, generatedId)
                                 .Create();

        _flightRepository.Setup(r => r.GetDtoByIdAsync(generatedId, _ct))
                         .ReturnsAsync(createdDto);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(createdDto);

        mappedFlight.AirlineId.Should().Be(airline.AirlineId);
        mappedFlight.OriginAirportId.Should().Be(originAirport.AirportId);
        mappedFlight.DestinationAirportId.Should().Be(destinationAirport.AirportId);
        mappedFlight.DefaultAircraftId.Should().Be(defaultAircraft.AircraftId);

        _mapper.Verify(m => m.Map<Flight>(dto), Times.Once);
        _flightRepository.Verify(r => r.AddAndSaveAsync(mappedFlight, _ct), Times.Once);
        _flightRepository.Verify(r => r.GetDtoByIdAsync(generatedId, _ct), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotSave()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateFlightDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid DTO" }
        };
        var dtoValidationResult = new Result(errors);

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        // Act
        var result = await _sut.UpdateAsync(flightId, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(errors);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenFlightNotFound_ReturnsNotFoundFailureAndDoesNotSave()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateFlightDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync((Flight?)null);

        // Act
        var result = await _sut.UpdateAsync(flightId, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Flight with ID '{flightId}' not found.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAirlineNotFound_ReturnsValidationFailureAndDoesNotSave()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateFlightDto>()
                          .With(d => d.DefaultAircraftTail, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync(_fixture.Create<Flight>());

        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync((Airline?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        // Act
        var result = await _sut.UpdateAsync(flightId, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airline with IataCode '{dto.AirlineIataCode}' not found.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenFlightNumberAlreadyExistsForAirline_ReturnsValidationFailureAndDoesNotSave()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateFlightDto>()
                          .With(d => d.DefaultAircraftTail, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync(_fixture.Create<Flight>());

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync(new Flight { FlightId = flightId + 1, AirlineId = airline.AirlineId, FlightNumber = dto.FlightNumber });

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        // Act
        var result = await _sut.UpdateAsync(flightId, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Flight with Number '{dto.FlightNumber}' already exists for Airline '{airline.Iatacode}'");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenOriginAirportNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateFlightDto>()
                          .With(d => d.DefaultAircraftTail, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync(_fixture.Create<Flight>());

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync((Flight?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync((Airport?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        // Act
        var result = await _sut.UpdateAsync(flightId, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airport with IataCode '{dto.OriginAirportIataCode}' not found.");

        _flightRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDestinationAirportNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateFlightDto>()
                          .With(d => d.DefaultAircraftTail, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync(_fixture.Create<Flight>());

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync((Flight?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync((Airport?)null);

        // Act
        var result = await _sut.UpdateAsync(flightId, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airport with IataCode '{dto.DestinationAirportIataCode}' not found.");

        _flightRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDefaultAircraftTailProvidedButAircraftNotFound_ReturnsValidationFailureAndDoesNotSave()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateFlightDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync(_fixture.Create<Flight>());

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync((Flight?)null);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.DefaultAircraftTail!, _ct))
                           .ReturnsAsync((Aircraft?)null);

        // Act
        var result = await _sut.UpdateAsync(flightId, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Aircraft with TailNumber '{dto.DefaultAircraftTail}' not found.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenFlightNumberBelongsToSameFlight_UpdatesFlightAndSavesChanges()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateFlightDto>()
                          .With(d => d.DefaultAircraftTail, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var existingFlight = _fixture.Build<Flight>()
                                     .With(f => f.FlightId, flightId)
                                     .Create();

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync(existingFlight);

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync(new Flight { FlightId = flightId, AirlineId = airline.AirlineId, FlightNumber = dto.FlightNumber });

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(_fixture.Create<Airport>());

        _mapper.Setup(m => m.Map(dto, existingFlight))
               .Returns(existingFlight);

        _flightRepository.Setup(r => r.UpdateAsync(existingFlight, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(flightId, dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        _mapper.Verify(m => m.Map(dto, existingFlight), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAllValidationsPass_UpdatesFlightAndSavesChanges()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateFlightDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var existingFlight = _fixture.Build<Flight>()
                                     .With(f => f.FlightId, flightId)
                                     .Create();

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync(existingFlight);

        var airline = _fixture.Create<Airline>();
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.AirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        _flightRepository.Setup(r => r.GetByAirlineIdAndFlightNumberAsync(airline.AirlineId, dto.FlightNumber, _ct))
                         .ReturnsAsync((Flight?)null);

        var originAirport = _fixture.Create<Airport>();
        var destinationAirport = _fixture.Create<Airport>();
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.OriginAirportIataCode, _ct))
                          .ReturnsAsync(originAirport);
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.DestinationAirportIataCode, _ct))
                          .ReturnsAsync(destinationAirport);

        var aircraft = _fixture.Build<Aircraft>()
                               .With(a => a.TailNumber, dto.DefaultAircraftTail)
                               .Create();

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.DefaultAircraftTail!, _ct))
                           .ReturnsAsync(aircraft);

        _mapper.Setup(m => m.Map(dto, existingFlight))
               .Returns(existingFlight);

        _flightRepository.Setup(r => r.UpdateAsync(existingFlight, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(flightId, dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        existingFlight.AirlineId.Should().Be(airline.AirlineId);
        existingFlight.OriginAirportId.Should().Be(originAirport.AirportId);
        existingFlight.DestinationAirportId.Should().Be(destinationAirport.AirportId);
        existingFlight.DefaultAircraftId.Should().Be(aircraft.AircraftId);

        _mapper.Verify(m => m.Map(dto, existingFlight), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion

    #region DeleteByIdAsync Tests

    [Fact]
    public async Task DeleteByIdAsync_WhenFlightNotFound_ReturnsNotFoundFailureAndDoesNotRemove()
    {
        // Arrange
        var flightId = _fixture.Create<int>();

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync((Flight?)null);

        // Act
        var result = await _sut.DeleteByIdAsync(flightId, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Flight with ID '{flightId}' not found.");

        _flightRepository.Verify(r => r.RemoveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteByIdAsync_WhenHasAssociatedSchedules_ReturnsValidationFailureAndDoesNotRemove()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var existingFlight = _fixture.Build<Flight>()
                                     .With(f => f.FlightId, flightId)
                                     .Create();

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync(existingFlight);

        _flightRepository.Setup(r => r.HasAnyFlightSchedulesAsync(flightId, _ct))
                         .ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteByIdAsync(flightId, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Flight with ID '{flightId}' cannot be deleted because it has associated schedules.");

        _flightRepository.Verify(r => r.RemoveAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteByIdAsync_WhenNoAssociatedSchedules_RemovesFlightAndSavesChanges()
    {
        // Arrange
        var flightId = _fixture.Create<int>();
        var existingFlight = _fixture.Build<Flight>()
                                     .With(f => f.FlightId, flightId)
                                     .Create();

        _flightRepository.Setup(r => r.GetByIdAsync(flightId, _ct))
                         .ReturnsAsync(existingFlight);

        _flightRepository.Setup(r => r.HasAnyFlightSchedulesAsync(flightId, _ct))
                         .ReturnsAsync(false);

        _flightRepository.Setup(r => r.RemoveAsync(existingFlight, _ct))
                         .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteByIdAsync(flightId, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        _flightRepository.Verify(r => r.RemoveAsync(existingFlight, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion
}
