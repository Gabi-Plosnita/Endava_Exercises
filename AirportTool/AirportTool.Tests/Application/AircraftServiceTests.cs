using AirportTool.Application;
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
        _dtoValidator = new Mock<IDtoValidator>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _logger = new Mock<ILogger<AircraftService>>();

        _unitOfWork.SetupGet(x => x.Aircrafts).Returns(_aircraftRepository.Object);

        _sut = new AircraftService(_unitOfWork.Object,
                                   _dtoValidator.Object,
                                   _mapper.Object,
                                   _logger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAircraftNotFound_ReturnsNotFoundErrorAndFailure()
    {
        // Arrange
        const int id = 123;
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
        const int id = 5;
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

        var validationResult = new Result();
        var validationErrors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "AirlineIataCode is too long." }
        };
        validationResult.Errors.AddRange(validationErrors);

        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(validationResult);

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

        var validationResult = new Result();
        _dtoValidator.Setup(v => v.Validate(dto))
                     .Returns(validationResult);

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
}
