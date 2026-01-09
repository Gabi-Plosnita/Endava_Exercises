using AirportTool.Application;
using AirportTool.Domain;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AirportTool.Tests;

public class AircraftService_GetByIdAsync_Tests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IAircraftRepository> _aircraftRepository;
    private readonly Mock<IAirlineRepository> _airlineRepository;
    private readonly Mock<IDtoValidator> _dtoValidator;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ILogger<AircraftService>> _logger;
    private readonly AircraftService _sut;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public AircraftService_GetByIdAsync_Tests()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _aircraftRepository = new Mock<IAircraftRepository>(MockBehavior.Strict);
        _airlineRepository = new Mock<IAirlineRepository>(MockBehavior.Strict);
        _dtoValidator = new Mock<IDtoValidator>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _logger = new Mock<ILogger<AircraftService>>();

        _unitOfWork.SetupGet(x => x.Aircrafts).Returns(_aircraftRepository.Object);
        _unitOfWork.SetupGet(x => x.Airlines).Returns(_airlineRepository.Object);

        _sut = new AircraftService(_unitOfWork.Object,
                                   _dtoValidator.Object,
                                   _mapper.Object,
                                   _logger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAircraftNotFound_ReturnsNotFoundErrorAndFailure()
    {
        // Arrange
        var id = 123;
        _aircraftRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
                           .ReturnsAsync((GetAircraftDto?)null);

        // Act
        var result = await _sut.GetByIdAsync(id, _ct);

        // Assert 
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Type.Should().Be(ErrorType.NotFound);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenAircraftFound_ReturnsDtoAndSuccess()
    {
        // Arrange
        var id = 5;
        var dto = new GetAircraftDto { AircraftId = id };

        _aircraftRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
                           .ReturnsAsync(dto);

        // Act
        var result = await _sut.GetByIdAsync(id, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetByFilterAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotCallRepository()
    {
        // Arrange
        var dto = new AircraftFilterDto
        {
            PageIndex = 0,
            PageSize = 10,
            AirlineIataCode = "TOO_LONG" 
        };

        var dtoValidationResult = new Result();
        var validationErrors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "AirlineIataCode is too long." }
        };
        dtoValidationResult.Errors.AddRange(validationErrors);

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        // Act
        var result = await _sut.GetByFilterAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull(); 
        result.Errors.Should().BeEquivalentTo(validationErrors);

        _aircraftRepository.Verify(
            r => r.GetDtoByFilterAsync(It.IsAny<AircraftFilterDto>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _dtoValidator.Verify(v => v.Validate(dto), Times.Once);
    }

    [Fact]
    public async Task GetByFilterAsync_WhenDtoIsValid_ReturnsPagedResultFromRepository()
    {
        // Arrange
        var dto = new AircraftFilterDto
        {
            PageIndex = 1,
            PageSize = 2,
            AirlineIataCode = "LH"
        };

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var pagedResult = _fixture.Build<PagedResult<GetAircraftDto>>()
                                  .With(p => p.PageIndex, dto.PageIndex)
                                  .With(p => p.PageSize, dto.PageSize)
                                  .With(p => p.Items, _fixture.CreateMany<GetAircraftDto>(3).ToList())
                                  .Create();

        _aircraftRepository.Setup(r => r.GetDtoByFilterAsync(dto, _ct))
                           .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetByFilterAsync(dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(pagedResult);

        _dtoValidator.Verify(v => v.Validate(dto), Times.Once);
        _aircraftRepository.Verify(r => r.GetDtoByFilterAsync(dto, _ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotPersist()
    {
        // Arrange
        var tailNumber = "YR-VAL-1";
        var dto = _fixture.Build<CreateAircraftDto>()
                          .With(d => d.TailNumber, tailNumber)
                          .Create();

        var validationErrors = new List<Error>
        {
            new Error { Type = ErrorType.Validation, Message = "Invalid DTO" }
        };
        var dtoValidationResult = new Result();
        dtoValidationResult.Errors.AddRange(validationErrors);

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(validationErrors);

        _aircraftRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Aircraft>(), It.IsAny<CancellationToken>()), Times.Never);
        _aircraftRepository.Verify(r => r.GetDtoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _dtoValidator.Verify(v => v.Validate(dto), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenTailNumberAlreadyExists_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var tailNumber = "DUP-1";
        var dto = _fixture.Build<CreateAircraftDto>()
                          .With(d => d.TailNumber, tailNumber)
                          .With(d => d.OwnedByAirlineIataCode, (string?)null) 
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(tailNumber, _ct))
                           .ReturnsAsync(new Aircraft { AircraftId = 99, TailNumber = tailNumber });

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();

        result.Errors.Should().ContainSingle(e => 
            e.Type == ErrorType.Validation &&
            e.Message == $"An aircraft with tail number '{tailNumber}' already exists.");

        _aircraftRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Aircraft>(), It.IsAny<CancellationToken>()), Times.Never);
        _aircraftRepository.Verify(r => r.GetDtoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

        _dtoValidator.Verify(v => v.Validate(dto), Times.Once);
        _aircraftRepository.Verify(r => r.GetByTailNumberAsync(tailNumber, _ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenOwnedByAirlineIataCodeNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var tailNumber = "YR-NEW-1";
        var ownedByAirlineIataCode = "LH";
        var dto = _fixture.Build<CreateAircraftDto>()
                          .With(d => d.TailNumber, tailNumber)
                          .With(d => d.OwnedByAirlineIataCode, ownedByAirlineIataCode)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(tailNumber, _ct))
                           .ReturnsAsync((Aircraft?)null);

        _airlineRepository.Setup(r => r.GetByIataCodeAsync(ownedByAirlineIataCode, _ct))
                          .ReturnsAsync((Airline?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();

        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airline with IATA code '{ownedByAirlineIataCode}' not found.");

        _aircraftRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Aircraft>(), It.IsAny<CancellationToken>()), Times.Never);
        _aircraftRepository.Verify(r => r.GetDtoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

        _dtoValidator.Verify(v => v.Validate(dto), Times.Once);
        _aircraftRepository.Verify(r => r.GetByTailNumberAsync(tailNumber, _ct), Times.Once);
        _airlineRepository.Verify(r => r.GetByIataCodeAsync(ownedByAirlineIataCode, _ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCreatedAircraftNotRetrievable_ReturnsUnexpectedFailure()
    {
        // Arrange
        var tailNumber = "YR-NEW-2";
        var dto = _fixture.Build<CreateAircraftDto>()
                          .With(d => d.TailNumber, tailNumber)
                          .With(d => d.OwnedByAirlineIataCode, (string?)null) 
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(tailNumber, _ct))
                           .ReturnsAsync((Aircraft?)null);

        var mappedAircraft = new Aircraft { TailNumber = tailNumber };
        _mapper.Setup(m => m.Map<Aircraft>(dto))
               .Returns(mappedAircraft);

        var generatedId = 123;
        _aircraftRepository.Setup(r => r.AddAndSaveAsync(mappedAircraft, _ct))
                           .Returns(Task.CompletedTask)
                           .Callback(() => mappedAircraft.AircraftId = generatedId);

        _aircraftRepository.Setup(r => r.GetDtoByIdAsync(generatedId, _ct))
                           .ReturnsAsync((GetAircraftDto?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();

        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Unexpected &&
            e.Message == "Aircraft not found after creation.");

        _aircraftRepository.Verify(r => r.AddAndSaveAsync(mappedAircraft, _ct), Times.Once);
        _aircraftRepository.Verify(r => r.GetDtoByIdAsync(generatedId, _ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAllValidationsPass_PersistsAircraftAndReturnsCreatedDto()
    {
        // Arrange
        var tailNumber = "YR-OK-1";
        var ownedByAirlineIataCode = "LH";
        var dto = _fixture.Build<CreateAircraftDto>()
                          .With(d => d.TailNumber, tailNumber)
                          .With(d => d.OwnedByAirlineIataCode, ownedByAirlineIataCode)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(tailNumber, _ct))
                           .ReturnsAsync((Aircraft?)null);

        var airlineId = 10;
        var airline = new Airline { AirlineId = airlineId, Iatacode = ownedByAirlineIataCode, Name = "Lufthansa" };
        _airlineRepository.Setup(r => r.GetByIataCodeAsync(ownedByAirlineIataCode, _ct))
                          .ReturnsAsync(airline);

        var mappedAircraft = new Aircraft { TailNumber = tailNumber };
        _mapper.Setup(m => m.Map<Aircraft>(dto))
               .Returns(mappedAircraft);

        var generatedId = 777;
        _aircraftRepository.Setup(r => r.AddAndSaveAsync(mappedAircraft, _ct))
                           .Returns(Task.CompletedTask)
                           .Callback(() => mappedAircraft.AircraftId = generatedId);

        var getAircraftDto = _fixture.Build<GetAircraftDto>()
                                     .With(d => d.AircraftId, generatedId)
                                     .With(d => d.TailNumber, tailNumber)
                                     .Create();

        _aircraftRepository.Setup(r => r.GetDtoByIdAsync(777, _ct))
                           .ReturnsAsync(getAircraftDto);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(getAircraftDto);

        mappedAircraft.OwnedByAirlineId.Should().Be(10);

        _aircraftRepository.Verify(r => r.AddAndSaveAsync(mappedAircraft, _ct), Times.Once);
        _aircraftRepository.Verify(r => r.GetDtoByIdAsync(generatedId, _ct), Times.Once);
        _airlineRepository.Verify(r => r.GetByIataCodeAsync(ownedByAirlineIataCode, _ct), Times.Once);
    }
}
