using AirportTool.Application;
using AirportTool.WebApi;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace AirportTool.Tests.Api.Controllers;

public class AircraftsControllerTests
{
    private readonly Mock<IAircraftService> _aircraftService;
    private readonly Mock<IResultSeverityResolver> _severityResolver;
    private readonly AircraftsController _sut;

    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public AircraftsControllerTests()
    {
        _aircraftService = new Mock<IAircraftService>(MockBehavior.Strict);
        _severityResolver = new Mock<IResultSeverityResolver>(MockBehavior.Strict);

        _sut = new AircraftsController(_aircraftService.Object, _severityResolver.Object);
    }

    #region GetById

    [Fact]
    public async Task GetById_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var errors = new List<Error>
        {
            new() { Type = ErrorType.NotFound, Message = "not found" }
        };
        var serviceResult = new Result<GetAircraftDto?>(errors);

        _aircraftService.Setup(s => s.GetByIdAsync(id, _ct))
                        .ReturnsAsync(serviceResult);

        var httpStatus = HttpStatusCode.NotFound;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(httpStatus);

        // Act
        var actionResult = await _sut.GetById(id, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)httpStatus);
        obj.Value.Should().BeSameAs(serviceResult.Errors);

        _severityResolver.Verify(r => r.GetHttpStatusCode(serviceResult), Times.Once);
        _aircraftService.Verify(s => s.GetByIdAsync(id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceReturnsSuccess_ReturnsOkWithValue()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<GetAircraftDto>()
                          .With(d => d.AircraftId, id)
                          .Create();

        var serviceResult = new Result<GetAircraftDto?> { Value = dto };

        _aircraftService.Setup(s => s.GetByIdAsync(id, _ct))
                        .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.GetById(id, _ct);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeSameAs(dto);

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region GetByFilter

    [Fact]
    public async Task GetByFilter_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var dto = _fixture.Create<AircraftFilterDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "bad request" }
        };
        var serviceResult = new Result<PagedResult<GetAircraftDto>>(errors);

        _aircraftService.Setup(s => s.GetByFilterAsync(dto, _ct))
                        .ReturnsAsync(serviceResult);

        var httpStatus = HttpStatusCode.BadRequest;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(httpStatus);

        // Act
        var actionResult = await _sut.GetByFilter(dto, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)httpStatus);
        obj.Value.Should().BeSameAs(serviceResult.Errors);

        _severityResolver.Verify(r => r.GetHttpStatusCode(serviceResult), Times.Once);
        _aircraftService.Verify(s => s.GetByFilterAsync(dto, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByFilter_WhenServiceReturnsSuccess_ReturnsOkWithValue()
    {
        // Arrange
        var dto = _fixture.Create<AircraftFilterDto>();

        var pagedResult = _fixture.Build<PagedResult<GetAircraftDto>>()
                            .With(p => p.PageIndex, dto.PageIndex)
                            .With(p => p.PageSize, dto.PageSize)
                            .With(p => p.Items, _fixture.CreateMany<GetAircraftDto>(2).ToList())
                            .Create();

        var serviceResult = new Result<PagedResult<GetAircraftDto>> { Value = pagedResult };

        _aircraftService.Setup(s => s.GetByFilterAsync(dto, _ct))
                        .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.GetByFilter(dto, _ct);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeSameAs(pagedResult);

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var dto = _fixture.Create<CreateAircraftDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "invalid dto" }
        };
        var serviceResult = new Result<GetAircraftDto?>(errors);

        _aircraftService.Setup(s => s.CreateAsync(dto, _ct))
                        .ReturnsAsync(serviceResult);

        var httpStatus = HttpStatusCode.BadRequest;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(httpStatus);

        // Act
        var actionResult = await _sut.Create(dto, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)httpStatus);
        obj.Value.Should().BeSameAs(serviceResult.Errors);

        _severityResolver.Verify(r => r.GetHttpStatusCode(serviceResult), Times.Once);
        _aircraftService.Verify(s => s.CreateAsync(dto, _ct), Times.Once);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsSuccessButValueIsNull_Returns500WithUnexpectedError()
    {
        // Arrange
        var dto = _fixture.Create<CreateAircraftDto>();

        var serviceResult = new Result<GetAircraftDto?> { Value = null };
        serviceResult.IsSuccessful.Should().BeTrue(); 

        _aircraftService.Setup(s => s.CreateAsync(dto, _ct))
                        .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.Create(dto, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        var returnedErrors = obj.Value.Should().BeAssignableTo<List<Error>>().Subject;
        returnedErrors.Should().ContainSingle(e =>
            e.Type == ErrorType.Unexpected &&
            e.Message == "Aircraft was created but could not be retrieved.");

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsSuccessWithValue_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = _fixture.Create<CreateAircraftDto>();

        var aircraftId = _fixture.Create<int>();
        var created = _fixture.Build<GetAircraftDto>()
                              .With(d => d.AircraftId, aircraftId)
                              .Create();

        var serviceResult = new Result<GetAircraftDto?> { Value = created };

        _aircraftService.Setup(s => s.CreateAsync(dto, _ct))
                        .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.Create(dto, _ct);

        // Assert
        var createdAt = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdAt.ActionName.Should().Be(nameof(AircraftsController.GetById));
        createdAt.Value.Should().BeSameAs(created);

        createdAt.RouteValues.Should().NotBeNull();
        createdAt.RouteValues!["id"].Should().Be(aircraftId);

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateAircraftDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.NotFound, Message = "not found" }
        };
        var serviceResult = new Result(errors);

        _aircraftService.Setup(s => s.UpdateAsync(id, dto, _ct))
                        .ReturnsAsync(serviceResult);

        var httpStatus = HttpStatusCode.NotFound;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(httpStatus);

        // Act
        var actionResult = await _sut.Update(id, dto, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)httpStatus);
        obj.Value.Should().BeSameAs(serviceResult.Errors);

        _severityResolver.Verify(r => r.GetHttpStatusCode(serviceResult), Times.Once);
        _aircraftService.Verify(s => s.UpdateAsync(id, dto, _ct), Times.Once);
    }

    [Fact]
    public async Task Update_WhenServiceReturnsSuccess_ReturnsNoContent()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateAircraftDto>();

        _aircraftService.Setup(s => s.UpdateAsync(id, dto, _ct))
                        .ReturnsAsync(new Result());

        // Act
        var actionResult = await _sut.Update(id, dto, _ct);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var id = _fixture.Create<int>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.NotFound, Message = "not found" }
        };
        var serviceResult = new Result(errors);

        _aircraftService.Setup(s => s.DeleteByIdAsync(id, _ct))
                        .ReturnsAsync(serviceResult);

        var httpStatus = HttpStatusCode.NotFound;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(httpStatus);

        // Act
        var actionResult = await _sut.Delete(id, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)httpStatus);
        obj.Value.Should().BeSameAs(serviceResult.Errors);

        _severityResolver.Verify(r => r.GetHttpStatusCode(serviceResult), Times.Once);
        _aircraftService.Verify(s => s.DeleteByIdAsync(id, _ct), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsSuccess_ReturnsNoContent()
    {
        // Arrange
        var id = _fixture.Create<int>();

        _aircraftService.Setup(s => s.DeleteByIdAsync(id, _ct))
                        .ReturnsAsync(new Result());

        // Act
        var actionResult = await _sut.Delete(id, _ct);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
        _aircraftService.Verify(s => s.DeleteByIdAsync(id, _ct), Times.Once);
    }

    #endregion
}
