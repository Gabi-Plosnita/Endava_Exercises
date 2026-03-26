using AirportTool.Application;
using AirportTool.WebApi;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace AirportTool.Tests.Api.Controllers;

public class BookingsControllerTests
{
    private readonly Mock<IBookingService> _bookingService;
    private readonly Mock<IResultSeverityResolver> _severityResolver;
    private readonly BookingsController _sut;

    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public BookingsControllerTests()
    {
        _bookingService = new Mock<IBookingService>(MockBehavior.Strict);
        _severityResolver = new Mock<IResultSeverityResolver>(MockBehavior.Strict);

        _sut = new BookingsController(_bookingService.Object, _severityResolver.Object);
    }

    #region GetByCode

    [Fact]
    public async Task GetByCode_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var code = _fixture.Create<string>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.NotFound, Message = "Booking not found" }
        };
        var serviceResult = new Result<GetBookingDto?>(errors);

        _bookingService.Setup(s => s.GetBookingByCodeAsync(code, _ct))
                       .ReturnsAsync(serviceResult);

        var httpStatus = HttpStatusCode.NotFound;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(httpStatus);

        // Act
        var actionResult = await _sut.GetByCode(code, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)httpStatus);
        obj.Value.Should().BeSameAs(serviceResult.Errors);

        _bookingService.Verify(s => s.GetBookingByCodeAsync(code, _ct), Times.Once);
        _severityResolver.Verify(r => r.GetHttpStatusCode(serviceResult), Times.Once);
    }

    [Fact]
    public async Task GetByCode_WhenServiceReturnsSuccess_ReturnsOkWithValue()
    {
        // Arrange
        var code = _fixture.Create<string>();

        var dto = _fixture.Build<GetBookingDto>()
                          .With(d => d.ConfirmationCode, code)
                          .Create();

        var serviceResult = new Result<GetBookingDto?> { Value = dto };

        _bookingService.Setup(s => s.GetBookingByCodeAsync(code, _ct))
                       .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.GetByCode(code, _ct);

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
        var dto = _fixture.Create<CreateBookingDto>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Validation, Message = "Invalid DTO" }
        };
        var serviceResult = new Result<GetBookingDto?>(errors);

        _bookingService.Setup(s => s.CreateBookingAsync(dto, _ct))
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

        _bookingService.Verify(s => s.CreateBookingAsync(dto, _ct), Times.Once);
        _severityResolver.Verify(r => r.GetHttpStatusCode(serviceResult), Times.Once);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsSuccess_ReturnsCreatedAtActionWithConfirmationCodeRoute()
    {
        // Arrange
        var dto = _fixture.Create<CreateBookingDto>();
        var confirmationCode = _fixture.Create<string>();

        var createdBooking = _fixture.Build<GetBookingDto>()
                                     .With(b => b.ConfirmationCode, confirmationCode)
                                     .Create();

        var serviceResult = new Result<GetBookingDto?> { Value = createdBooking };

        _bookingService.Setup(s => s.CreateBookingAsync(dto, _ct))
                       .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.Create(dto, _ct);

        // Assert
        var createdAt = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdAt.ActionName.Should().Be(nameof(BookingsController.GetByCode));
        createdAt.Value.Should().BeSameAs(createdBooking);

        createdAt.RouteValues.Should().NotBeNull();
        createdAt.RouteValues!["code"].Should().Be(confirmationCode);

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region Cancel

    [Fact]
    public async Task Cancel_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var code = _fixture.Create<string>();

        var errors = new List<Error>
        {
            new() { Type = ErrorType.Conflict, Message = "Concurrency conflict" }
        };
        var serviceResult = new Result(errors);

        _bookingService.Setup(s => s.CancelBookingAsync(code, _ct))
                       .ReturnsAsync(serviceResult);

        var httpStatus = HttpStatusCode.Conflict;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(httpStatus);

        // Act
        var actionResult = await _sut.Cancel(code, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)httpStatus);
        obj.Value.Should().BeSameAs(serviceResult.Errors);

        _bookingService.Verify(s => s.CancelBookingAsync(code, _ct), Times.Once);
        _severityResolver.Verify(r => r.GetHttpStatusCode(serviceResult), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenServiceReturnsSuccess_ReturnsNoContent()
    {
        // Arrange
        var code = _fixture.Create<string>();

        _bookingService.Setup(s => s.CancelBookingAsync(code, _ct))
                       .ReturnsAsync(new Result());

        // Act
        var actionResult = await _sut.Cancel(code, _ct);

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion
}
