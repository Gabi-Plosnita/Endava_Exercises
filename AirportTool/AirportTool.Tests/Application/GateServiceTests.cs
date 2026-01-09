using AirportTool.Application;
using AirportTool.Domain;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AirportTool.Tests.Application;

public class GateService_Tests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IGateRepository> _gateRepository;
    private readonly Mock<IAirportRepository> _airportRepository;
    private readonly Mock<IDtoValidator> _dtoValidator;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ILogger<GateService>> _logger;

    private readonly GateService _sut;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public GateService_Tests()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _gateRepository = new Mock<IGateRepository>(MockBehavior.Strict);
        _airportRepository = new Mock<IAirportRepository>(MockBehavior.Strict);
        _dtoValidator = new Mock<IDtoValidator>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _logger = new Mock<ILogger<GateService>>();

        _unitOfWork.SetupGet(x => x.Gates).Returns(_gateRepository.Object);
        _unitOfWork.SetupGet(x => x.Airports).Returns(_airportRepository.Object);

        _sut = new GateService(
            _unitOfWork.Object,
            _dtoValidator.Object,
            _mapper.Object,
            _logger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenGateNotFound_ReturnsNotFoundErrorAndFailure()
    {
        // Arrange
        var id = _fixture.Create<int>();

        _gateRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
                       .ReturnsAsync((GetGateDto?)null);

        // Act
        var result = await _sut.GetByIdAsync(id, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Gate with Id {id} not found.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenGateFound_ReturnsDtoAndSuccess()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<GetGateDto>()
                          .With(d => d.GateId, id)
                          .Create();

        _gateRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
                       .ReturnsAsync(dto);

        // Act
        var result = await _sut.GetByIdAsync(id, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(dto);
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<CreateGateDto>();

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

        _gateRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Gate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenAirportNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<CreateGateDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.AirportIataCode, _ct))
                          .ReturnsAsync((Airport?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airport with Iata Code {dto.AirportIataCode} not found.");

        _gateRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Gate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenGateCodeAlreadyExistsForAirport_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<CreateGateDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var airport = _fixture.Create<Airport>();
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.AirportIataCode, _ct))
                          .ReturnsAsync(airport);

        _gateRepository.Setup(r => r.GetByAirportIdAndCodeAsync(airport.AirportId, dto.Code, _ct))
                       .ReturnsAsync(new Gate { GateId = _fixture.Create<int>(), AirportId = airport.AirportId, Code = dto.Code });

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Gate with Code {dto.Code} already exists for Airport {airport.AirportId}.");

        _gateRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Gate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCreatedGateNotRetrievable_ReturnsUnexpectedFailure()
    {
        // Arrange
        var dto = _fixture.Create<CreateGateDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var airport = _fixture.Create<Airport>();
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.AirportIataCode, _ct))
                          .ReturnsAsync(airport);

        _gateRepository.Setup(r => r.GetByAirportIdAndCodeAsync(airport.AirportId, dto.Code, _ct))
                       .ReturnsAsync((Gate?)null);

        var mappedGate = new Gate { Code = dto.Code };
        _mapper.Setup(m => m.Map<Gate>(dto))
               .Returns(mappedGate);

        var generatedGateId = _fixture.Create<int>();
        _gateRepository.Setup(r => r.AddAndSaveAsync(mappedGate, _ct))
                       .Returns(Task.CompletedTask)
                       .Callback(() =>
                       {
                           mappedGate.GateId = generatedGateId;
                       });

        _gateRepository.Setup(r => r.GetDtoByIdAsync(generatedGateId, _ct))
                       .ReturnsAsync((GetGateDto?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Unexpected &&
            e.Message == "Gate not found after creation");

        _gateRepository.Verify(r => r.AddAndSaveAsync(mappedGate, _ct), Times.Once);
        _gateRepository.Verify(r => r.GetDtoByIdAsync(generatedGateId, _ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAllValidationsPass_PersistsGateAndReturnsCreatedDto()
    {
        // Arrange
        var dto = _fixture.Create<CreateGateDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var airport = _fixture.Create<Airport>();
        _airportRepository.Setup(r => r.GetByIataCodeAsync(dto.AirportIataCode, _ct))
                          .ReturnsAsync(airport);

        _gateRepository.Setup(r => r.GetByAirportIdAndCodeAsync(airport.AirportId, dto.Code, _ct))
                       .ReturnsAsync((Gate?)null);

        var mappedGate = new Gate { Code = dto.Code };
        _mapper.Setup(m => m.Map<Gate>(dto))
               .Returns(mappedGate);

        var generatedGateId = _fixture.Create<int>();
        _gateRepository.Setup(r => r.AddAndSaveAsync(mappedGate, _ct))
                       .Returns(Task.CompletedTask)
                       .Callback(() =>
                       {
                           mappedGate.GateId = generatedGateId;
                       });

        var createdDto = _fixture.Build<GetGateDto>()
                                 .Create();

        _gateRepository.Setup(r => r.GetDtoByIdAsync(generatedGateId, _ct))
                       .ReturnsAsync(createdDto);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(createdDto);

        mappedGate.AirportId.Should().Be(airport.AirportId);

        _gateRepository.Verify(r => r.AddAndSaveAsync(mappedGate, _ct), Times.Once);
        _gateRepository.Verify(r => r.GetDtoByIdAsync(generatedGateId, _ct), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotSave()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateGateDto>().Create();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid DTO" }
        };
        var dtoValidationResult = new Result();
        dtoValidationResult.Errors.AddRange(errors);

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(errors);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenGateNotFound_ReturnsNotFoundFailureAndDoesNotSave()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateGateDto>().Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _gateRepository.Setup(r => r.GetByIdAsync(id, _ct))
                       .ReturnsAsync((Gate?)null);

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Gate with Id {id} not found.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenGateCodeAlreadyExistsForAirport_ReturnsValidationFailure()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateGateDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var existingGate = _fixture.Build<Gate>()
                                   .With(g => g.GateId, id)
                                   .Create();

        _gateRepository.Setup(r => r.GetByIdAsync(id, _ct))
                       .ReturnsAsync(existingGate);

        _gateRepository.Setup(r => r.GetByAirportIdAndCodeAsync(existingGate.AirportId, dto.Code, _ct))
                       .ReturnsAsync(new Gate { GateId = id + 1, AirportId = existingGate.AirportId, Code = dto.Code });

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Gate with Code {dto.Code} already exists for Airport {existingGate.AirportId}.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenGateCodeBelongsToSameGate_UpdatesGateAndSavesChanges()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateGateDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var existingGate = _fixture.Build<Gate>()
                                   .With(g => g.GateId, id)
                                   .Create();

        _gateRepository.Setup(r => r.GetByIdAsync(id, _ct))
                       .ReturnsAsync(existingGate);

        _gateRepository.Setup(r => r.GetByAirportIdAndCodeAsync(existingGate.AirportId, dto.Code, _ct))
                       .ReturnsAsync(new Gate { GateId = id, AirportId = existingGate.AirportId, Code = dto.Code });

        _mapper.Setup(m => m.Map(dto, existingGate))
               .Returns(existingGate);

        _gateRepository.Setup(r => r.UpdateAsync(existingGate, _ct))
                       .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAllValidationsPass_UpdatesGateAndSavesChanges()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateGateDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var existingGate = _fixture.Build<Gate>()
                                   .With(g => g.GateId, id)
                                   .Create();

        _gateRepository.Setup(r => r.GetByIdAsync(id, _ct))
                       .ReturnsAsync(existingGate);

        _gateRepository.Setup(r => r.GetByAirportIdAndCodeAsync(existingGate.AirportId, dto.Code, _ct))
                       .ReturnsAsync((Gate?)null);

        _mapper.Setup(m => m.Map(dto, existingGate))
               .Returns(existingGate);

        _gateRepository.Setup(r => r.UpdateAsync(existingGate, _ct))
                       .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion

    #region DeleteByIdAsync Tests

    [Fact]
    public async Task DeleteByIdAsync_WhenGateNotFound_ReturnsNotFoundFailureAndDoesNotRemove()
    {
        // Arrange
        var id = _fixture.Create<int>();

        _gateRepository.Setup(r => r.GetByIdAsync(id, _ct))
                       .ReturnsAsync((Gate?)null);

        // Act
        var result = await _sut.DeleteByIdAsync(id, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Gate with Id {id} not found.");

        _gateRepository.Verify(r => r.RemoveAsync(It.IsAny<Gate>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteByIdAsync_WhenGateFound_RemovesGateAndSavesChanges()
    {
        // Arrange
        var id = _fixture.Create<int>();

        var existingGate = _fixture.Build<Gate>()
                                   .With(g => g.GateId, id)
                                   .Create();

        _gateRepository.Setup(r => r.GetByIdAsync(id, _ct))
                       .ReturnsAsync(existingGate);

        _gateRepository.Setup(r => r.RemoveAsync(existingGate, _ct))
                       .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteByIdAsync(id, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        _gateRepository.Verify(r => r.RemoveAsync(existingGate, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion
}
