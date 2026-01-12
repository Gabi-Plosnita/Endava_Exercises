using AirportTool.Application;
using AirportTool.WebApi;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace AirportTool.Tests.WebApi;

public class TicketsControllerTests
{
    private readonly Mock<ITicketService> _ticketService;
    private readonly Mock<IResultSeverityResolver> _severityResolver;

    private readonly TicketsController _sut;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public TicketsControllerTests()
    {
        _ticketService = new Mock<ITicketService>(MockBehavior.Strict);
        _severityResolver = new Mock<IResultSeverityResolver>(MockBehavior.Strict);

        _sut = new TicketsController(_ticketService.Object, _severityResolver.Object);
    }

    #region GetById

    [Fact]
    public async Task GetById_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var id = _fixture.Create<long>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.NotFound, Message = "Ticket not found" }
        };
        var serviceResult = new Result<GetTicketDto?>(errors);

        _ticketService.Setup(s => s.GetByIdAsync(id, _ct))
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
        var id = _fixture.Create<long>();
        var dto = _fixture.Build<GetTicketDto>()
                          .With(d => d.TicketId, id)
                          .Create();

        var serviceResult = new Result<GetTicketDto?> { Value = dto };

        _ticketService.Setup(s => s.GetByIdAsync(id, _ct))
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

    #region GetByFlightSchedule

    [Fact]
    public async Task GetByFlightSchedule_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var flightScheduleId = _fixture.Create<int>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.NotFound, Message = "Flight schedule not found" }
        };
        var serviceResult = new Result<IReadOnlyList<GetTicketDto>>(errors);

        _ticketService.Setup(s => s.GetByFlightScheduleIdAsync(flightScheduleId, _ct))
                      .ReturnsAsync(serviceResult);

        var status = HttpStatusCode.NotFound;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(status);

        // Act
        var actionResult = await _sut.GetByFlightSchedule(flightScheduleId, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)status);
        obj.Value.Should().BeSameAs(serviceResult.Errors);
    }

    [Fact]
    public async Task GetByFlightSchedule_WhenServiceReturnsSuccess_ReturnsOkWithValue()
    {
        // Arrange
        var flightScheduleId = _fixture.Create<int>();

        var tickets = _fixture.CreateMany<GetTicketDto>(3).ToList().AsReadOnly();
        var serviceResult = new Result<IReadOnlyList<GetTicketDto>> { Value = tickets };

        _ticketService.Setup(s => s.GetByFlightScheduleIdAsync(flightScheduleId, _ct))
                      .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.GetByFlightSchedule(flightScheduleId, _ct);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeSameAs(tickets);

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var dto = _fixture.Create<CreateTicketDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid ticket" }
        };
        var serviceResult = new Result<GetTicketDto?>(errors);

        _ticketService.Setup(s => s.CreateAsync(dto, _ct))
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
        var dto = _fixture.Create<CreateTicketDto>();

        var id = _fixture.Create<long>();
        var created = _fixture.Build<GetTicketDto>()
                              .With(d => d.TicketId, id)
                              .Create();

        var serviceResult = new Result<GetTicketDto?> { Value = created };

        _ticketService.Setup(s => s.CreateAsync(dto, _ct))
                      .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.Create(dto, _ct);

        // Assert
        var createdAt = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdAt.ActionName.Should().Be(nameof(TicketsController.GetById));
        createdAt.Value.Should().BeSameAs(created);

        createdAt.RouteValues.Should().NotBeNull();
        createdAt.RouteValues!["id"].Should().Be(id);

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region UpdateInventory

    [Fact]
    public async Task UpdateInventory_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var id = _fixture.Create<long>();
        var dto = _fixture.Create<UpdateTicketDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.NotFound, Message = "Ticket missing" }
        };
        var serviceResult = new Result(errors);

        _ticketService.Setup(s => s.UpdateAsync(id, dto, _ct))
                      .ReturnsAsync(serviceResult);

        var status = HttpStatusCode.NotFound;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(status);

        // Act
        var actionResult = await _sut.UpdateInventory(id, dto, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)status);
        obj.Value.Should().BeSameAs(serviceResult.Errors);
    }

    [Fact]
    public async Task UpdateInventory_WhenServiceReturnsSuccess_ReturnsNoContent()
    {
        // Arrange
        var id = _fixture.Create<long>();
        var dto = _fixture.Create<UpdateTicketDto>();

        var serviceResult = new Result();
        _ticketService.Setup(s => s.UpdateAsync(id, dto, _ct))
                      .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.UpdateInventory(id, dto, _ct);

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
        var id = _fixture.Create<long>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Cannot delete" }
        };
        var serviceResult = new Result(errors);

        _ticketService.Setup(s => s.DeleteByIdAsync(id, _ct))
                      .ReturnsAsync(serviceResult);

        var status = HttpStatusCode.BadRequest;
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
        var id = _fixture.Create<long>();

        var serviceResult = new Result(); 
        _ticketService.Setup(s => s.DeleteByIdAsync(id, _ct))
                      .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.Delete(id, _ct);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion
}
