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

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenAircraftNotFound_ReturnsNotFoundErrorAndFailure()
    {
        // Arrange
        var id = _fixture.Create<int>();
        _aircraftRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
                           .ReturnsAsync((GetAircraftDto?)null);

        // Act
        var result = await _sut.GetByIdAsync(id, _ct);

        // Assert 
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Aircraft with ID {id} not found.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenAircraftFound_ReturnsDtoAndSuccess()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<GetAircraftDto>()
                          .With(d => d.AircraftId, id)
                          .Create();

        _aircraftRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
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
        var dto = _fixture.Create<AircraftFilterDto>();

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
    }

    [Fact]
    public async Task GetByFilterAsync_WhenDtoIsValid_ReturnsPagedResultFromRepository()
    {
        // Arrange
        var dto = _fixture.Create<AircraftFilterDto>();

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

        _aircraftRepository.Verify(r => r.GetDtoByFilterAsync(dto, _ct), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Build<CreateAircraftDto>().Create();

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
    }

    [Fact]
    public async Task CreateAsync_WhenTailNumberAlreadyExists_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Build<CreateAircraftDto>()
                          .With(d => d.OwnedByAirlineIataCode, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.TailNumber, _ct))
                           .ReturnsAsync(new Aircraft { AircraftId = 99, TailNumber = dto.TailNumber });

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();

        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"An aircraft with tail number '{dto.TailNumber}' already exists.");

        _aircraftRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Aircraft>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenOwnedByAirlineIataCodeNotFound_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        var dto = _fixture.Create<CreateAircraftDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.TailNumber, _ct))
                           .ReturnsAsync((Aircraft?)null);

        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.OwnedByAirlineIataCode!, _ct))
                          .ReturnsAsync((Airline?)null);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();

        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airline with IATA code '{dto.OwnedByAirlineIataCode}' not found.");

        _aircraftRepository.Verify(r => r.AddAndSaveAsync(It.IsAny<Aircraft>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCreatedAircraftNotRetrievable_ReturnsUnexpectedFailure()
    {
        // Arrange
        var dto = _fixture.Build<CreateAircraftDto>()
                          .With(d => d.OwnedByAirlineIataCode, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.TailNumber, _ct))
                           .ReturnsAsync((Aircraft?)null);

        var mappedAircraft = new Aircraft { TailNumber = dto.TailNumber };
        _mapper.Setup(m => m.Map<Aircraft>(dto))
               .Returns(mappedAircraft);

        var generatedId = _fixture.Create<int>();
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
        var dto = _fixture.Create<CreateAircraftDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.TailNumber, _ct))
                           .ReturnsAsync((Aircraft?)null);

        var airline = _fixture.Build<Airline>()
                              .With(a => a.Iatacode, dto.OwnedByAirlineIataCode)
                              .Create();

        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.OwnedByAirlineIataCode!, _ct))
                          .ReturnsAsync(airline);

        var mappedAircraft = new Aircraft { TailNumber = dto.TailNumber };
        _mapper.Setup(m => m.Map<Aircraft>(dto))
               .Returns(mappedAircraft);

        var generatedId = _fixture.Create<int>();
        _aircraftRepository.Setup(r => r.AddAndSaveAsync(mappedAircraft, _ct))
                           .Returns(Task.CompletedTask)
                           .Callback(() => mappedAircraft.AircraftId = generatedId);

        var getAircraftDto = _fixture.Build<GetAircraftDto>()
                                     .With(d => d.AircraftId, generatedId)
                                     .With(d => d.TailNumber, dto.TailNumber)
                                     .Create();

        _aircraftRepository.Setup(r => r.GetDtoByIdAsync(generatedId, _ct))
                           .ReturnsAsync(getAircraftDto);

        // Act
        var result = await _sut.CreateAsync(dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(getAircraftDto);

        mappedAircraft.OwnedByAirlineId.Should().Be(airline.AirlineId);

        _aircraftRepository.Verify(r => r.AddAndSaveAsync(mappedAircraft, _ct), Times.Once);
        _aircraftRepository.Verify(r => r.GetDtoByIdAsync(generatedId, _ct), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenDtoValidationFails_ReturnsFailureAndDoesNotSave()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateAircraftDto>().Create();

        var errors = new List<Error>
        {
            new Error { Type = ErrorType.Validation, Message = "Invalid DTO" }
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
    public async Task UpdateAsync_WhenAircraftNotFound_ReturnsNotFoundFailureAndDoesNotSave()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateAircraftDto>()
                          .With(d => d.OwnedByAirlineIataCode, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        _aircraftRepository.Setup(r => r.GetByIdAsync(id, _ct))
                           .ReturnsAsync((Aircraft?)null);

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Aircraft with ID {id} not found.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenTailNumberAlreadyUsedByAnotherAircraft_ReturnsValidationFailure()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateAircraftDto>()
                          .With(d => d.OwnedByAirlineIataCode, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var existing = new Aircraft { AircraftId = id };

        _aircraftRepository.Setup(r => r.GetByIdAsync(id, _ct))
                           .ReturnsAsync(existing);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.TailNumber, _ct))
                           .ReturnsAsync(new Aircraft { AircraftId = id + 1, TailNumber = dto.TailNumber });

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"An aircraft with tail number '{dto.TailNumber}' already exists.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenOwnedByAirlineIataCodeNotFound_ReturnsValidationFailure()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateAircraftDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var existing = new Aircraft { AircraftId = id };

        _aircraftRepository.Setup(r => r.GetByIdAsync(id, _ct))
                           .ReturnsAsync(existing);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.TailNumber, _ct))
                           .ReturnsAsync((Aircraft?)null);

        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.OwnedByAirlineIataCode!, _ct))
                          .ReturnsAsync((Airline?)null);

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == $"Airline with IATA code '{dto.OwnedByAirlineIataCode}' not found.");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenTailNumberBelongsToSameAircraft_UpdatesAircraftAndSavesChanges()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<UpdateAircraftDto>()
                          .With(d => d.OwnedByAirlineIataCode, (string?)null)
                          .Create();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var existing = new Aircraft { AircraftId = id };

        _aircraftRepository.Setup(r => r.GetByIdAsync(id, _ct))
                           .ReturnsAsync(existing);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.TailNumber, _ct))
                           .ReturnsAsync(new Aircraft { AircraftId = id, TailNumber = dto.TailNumber });

        _mapper.Setup(m => m.Map(dto, existing))
               .Returns(existing);

        _aircraftRepository.Setup(r => r.UpdateAsync(existing, _ct))
                           .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAllValidationsPass_UpdatesAircraftAndSavesChanges()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateAircraftDto>();

        var dtoValidationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(dtoValidationResult);

        var existing = new Aircraft { AircraftId = id };

        _aircraftRepository.Setup(r => r.GetByIdAsync(id, _ct))
                           .ReturnsAsync(existing);

        _aircraftRepository.Setup(r => r.GetByTailNumberAsync(dto.TailNumber, _ct))
                           .ReturnsAsync((Aircraft?)null);

        var airline = _fixture.Build<Airline>()
                              .With(a => a.Iatacode, dto.OwnedByAirlineIataCode)
                              .Create();

        _airlineRepository.Setup(r => r.GetByIataCodeAsync(dto.OwnedByAirlineIataCode!, _ct))
                          .ReturnsAsync(airline);

        _mapper.Setup(m => m.Map(dto, existing))
               .Returns(existing);

        _aircraftRepository.Setup(r => r.UpdateAsync(existing, _ct))
                           .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(id, dto, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        existing.OwnedByAirlineId.Should().Be(airline.AirlineId);

        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteByIdAsync_WhenAircraftNotFound_ReturnsNotFoundFailureAndDoesNotRemove()
    {
        // Arrange
        var id = _fixture.Create<int>();

        _aircraftRepository.Setup(r => r.GetByIdAsync(id, _ct))
                           .ReturnsAsync((Aircraft?)null);

        // Act
        var result = await _sut.DeleteByIdAsync(id, _ct);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e =>
            e.Type == ErrorType.NotFound &&
            e.Message == $"Aircraft with ID {id} not found.");

        _aircraftRepository.Verify(r => r.RemoveAsync(It.IsAny<Aircraft>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteByIdAsync_WhenAircraftFound_RemovesAircraftAndSavesChanges()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var existing = _fixture.Build<Aircraft>()
                               .With(a => a.AircraftId, id)
                               .Create();

        _aircraftRepository.Setup(r => r.GetByIdAsync(id, _ct))
                           .ReturnsAsync(existing);

        _aircraftRepository.Setup(r => r.RemoveAsync(existing, _ct))
                           .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(_ct))
                   .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteByIdAsync(id, _ct);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _aircraftRepository.Verify(r => r.RemoveAsync(existing, _ct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(_ct), Times.Once);
    }

    #endregion
}
