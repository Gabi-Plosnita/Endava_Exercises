using AirportTool.Application;
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
        const int id = 123;
        _aircraftRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
                           .ReturnsAsync((GetAircraftDto?)null);

        var result = await _sut.GetByIdAsync(id, _ct);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Type.Should().Be(ErrorType.NotFound);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenAircraftFound_ReturnsDtoAndSuccess()
    {
        const int id = 5;
        var dto = new GetAircraftDto { AircraftId = id };

        _aircraftRepository.Setup(r => r.GetDtoByIdAsync(id, _ct))
                           .ReturnsAsync(dto);

        var result = await _sut.GetByIdAsync(id, _ct);

        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(dto);
    }
}
