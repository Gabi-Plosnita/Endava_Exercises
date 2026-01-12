using AirportTool.Application;
using AirportTool.WebApi;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace AirportTool.Tests.WebApi;

public class FlightsControllerTests
{
    private readonly Mock<IFlightService> _flightService;
    private readonly Mock<IResultSeverityResolver> _severityResolver;

    private readonly FlightsController _sut;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public FlightsControllerTests()
    {
        _flightService = new Mock<IFlightService>(MockBehavior.Strict);
        _severityResolver = new Mock<IResultSeverityResolver>(MockBehavior.Strict);

        _sut = new FlightsController(_flightService.Object, _severityResolver.Object);
    }

    #region GetById

    [Fact]
    public async Task GetById_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var id = _fixture.Create<int>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.NotFound, Message = "Flight not found" }
        };
        var serviceResult = new Result<GetFlightDto?>(errors);

        _flightService.Setup(s => s.GetByIdAsync(id, _ct))
                      .ReturnsAsync(serviceResult);

        var status = HttpStatusCode.NotFound;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(status);

        // Act
        var actionResult = await _sut.GetById(id, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)status);
        obj.Value.Should().BeSameAs(serviceResult.Errors);
    }

    [Fact]
    public async Task GetById_WhenServiceReturnsSuccess_ReturnsOkWithValue()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Build<GetFlightDto>()
                          .With(d => d.FlightId, id)
                          .Create();

        var serviceResult = new Result<GetFlightDto?> { Value = dto };

        _flightService.Setup(s => s.GetByIdAsync(id, _ct))
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

    #region Create

    [Fact]
    public async Task Create_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var dto = _fixture.Create<CreateFlightDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid DTO" }
        };
        var serviceResult = new Result<GetFlightDto?>(errors);

        _flightService.Setup(s => s.CreateAsync(dto, _ct))
                      .ReturnsAsync(serviceResult);

        var status = HttpStatusCode.BadRequest;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(status);

        // Act
        var actionResult = await _sut.Create(dto, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)status);
        obj.Value.Should().BeSameAs(serviceResult.Errors);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsSuccess_ReturnsCreatedAtActionWithRouteId()
    {
        // Arrange
        var dto = _fixture.Create<CreateFlightDto>();

        var id = _fixture.Create<int>();
        var created = _fixture.Build<GetFlightDto>()
                              .With(d => d.FlightId, id)
                              .Create();

        var serviceResult = new Result<GetFlightDto?> { Value = created };

        _flightService.Setup(s => s.CreateAsync(dto, _ct))
                      .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.Create(dto, _ct);

        // Assert
        var createdAt = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdAt.ActionName.Should().Be(nameof(FlightsController.GetById));
        createdAt.Value.Should().BeSameAs(created);

        createdAt.RouteValues.Should().NotBeNull();
        createdAt.RouteValues!["id"].Should().Be(id);

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateFlightDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid update" }
        };
        var serviceResult = new Result(errors);

        _flightService.Setup(s => s.UpdateAsync(id, dto, _ct))
                      .ReturnsAsync(serviceResult);

        var status = HttpStatusCode.BadRequest;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(status);

        // Act
        var actionResult = await _sut.Update(id, dto, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)status);
        obj.Value.Should().BeSameAs(serviceResult.Errors);
    }

    [Fact]
    public async Task Update_WhenServiceReturnsSuccess_ReturnsNoContent()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var dto = _fixture.Create<UpdateFlightDto>();

        var serviceResult = new Result(); 
        _flightService.Setup(s => s.UpdateAsync(id, dto, _ct))
                      .ReturnsAsync(serviceResult);

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
            new() { Type = ErrorType.NotFound, Message = "Not found" }
        };
        var serviceResult = new Result(errors);

        _flightService.Setup(s => s.DeleteByIdAsync(id, _ct))
                      .ReturnsAsync(serviceResult);

        var status = HttpStatusCode.NotFound;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(status);

        // Act
        var actionResult = await _sut.Delete(id, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)status);
        obj.Value.Should().BeSameAs(serviceResult.Errors);
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsSuccess_ReturnsNoContent()
    {
        // Arrange
        var id = _fixture.Create<int>();

        var serviceResult = new Result();
        _flightService.Setup(s => s.DeleteByIdAsync(id, _ct))
                      .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.Delete(id, _ct);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion
}
