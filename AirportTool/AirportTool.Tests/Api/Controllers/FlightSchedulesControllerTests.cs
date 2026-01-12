using System.Net;
using System.Text;
using System.Text.Json;
using AirportTool.Application;
using AirportTool.WebApi;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AirportTool.Tests.WebApi;

public class FlightSchedulesControllerTests
{
    private readonly Mock<IFlightSchedulesService> _flightSchedulesService;
    private readonly Mock<IResultSeverityResolver> _severityResolver;

    private readonly FlightSchedulesController _sut;
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public FlightSchedulesControllerTests()
    {
        _flightSchedulesService = new Mock<IFlightSchedulesService>(MockBehavior.Strict);
        _severityResolver = new Mock<IResultSeverityResolver>(MockBehavior.Strict);
        _fixture.Register(() => DateOnly.FromDateTime(_fixture.Create<DateTime>()));

        _sut = new FlightSchedulesController(_flightSchedulesService.Object, _severityResolver.Object);
    }

    #region GetById

    [Fact]
    public async Task GetById_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var id = _fixture.Create<int>();
        var errors = new List<Error> { new() { Type = ErrorType.NotFound, Message = "not found" } };
        var serviceResult = new Result<GetFlightScheduleDto?>(errors);

        _flightSchedulesService.Setup(s => s.GetByIdAsync(id, _ct))
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
        var dto = _fixture.Build<GetFlightScheduleDto>().Create();
        var serviceResult = new Result<GetFlightScheduleDto?> { Value = dto };

        _flightSchedulesService.Setup(s => s.GetByIdAsync(id, _ct))
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
        var filter = _fixture.Create<FlightScheduleFilterDto>();
        var errors = new List<Error> { new() { Type = ErrorType.Validation, Message = "bad filter" } };
        var serviceResult = new Result<PagedResult<FlightScheduleSearchDto>>(errors);

        _flightSchedulesService.Setup(s => s.GetByFilterAsync(filter, _ct))
                               .ReturnsAsync(serviceResult);

        var status = HttpStatusCode.BadRequest;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(status);

        // Act
        var actionResult = await _sut.GetByFilter(filter, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)status);
        obj.Value.Should().BeSameAs(serviceResult.Errors);
    }

    [Fact]
    public async Task GetByFilter_WhenServiceReturnsSuccess_ReturnsOkWithPagedResult()
    {
        // Arrange
        var filter = _fixture.Create<FlightScheduleFilterDto>();
        var paged = _fixture.Create<PagedResult<FlightScheduleSearchDto>>();
        var serviceResult = new Result<PagedResult<FlightScheduleSearchDto>> { Value = paged };

        _flightSchedulesService.Setup(s => s.GetByFilterAsync(filter, _ct))
                               .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.GetByFilter(filter, _ct);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(paged);

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region GetUpcomingStats

    [Fact]
    public async Task GetUpcomingStats_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var errors = new List<Error> { new() { Type = ErrorType.Validation, Message = "bad range" } };
        var serviceResult = new Result<IReadOnlyList<DailyFlightStatsDto>>(errors);

        _flightSchedulesService
            .Setup(s => s.GetDailyStatsAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), _ct))
            .ReturnsAsync(serviceResult);

        var status = HttpStatusCode.BadRequest;
        _severityResolver.Setup(r => r.GetHttpStatusCode(serviceResult))
                         .Returns(status);

        // Act
        var actionResult = await _sut.GetUpcomingStats(_ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be((int)status);
        obj.Value.Should().BeSameAs(serviceResult.Errors);
    }

    [Fact]
    public async Task GetUpcomingStats_WhenServiceReturnsSuccess_ReturnsOkWithStats()
    {
        // Arrange
        var stats = _fixture.CreateMany<DailyFlightStatsDto>(3).ToList().AsReadOnly();
        var serviceResult = new Result<IReadOnlyList<DailyFlightStatsDto>> { Value = stats };

        DateOnly? capturedStart = null;
        DateOnly? capturedEnd = null;

        _flightSchedulesService
            .Setup(s => s.GetDailyStatsAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), _ct))
            .Callback<DateOnly, DateOnly, CancellationToken>((s, e, _) =>
            {
                capturedStart = s;
                capturedEnd = e;
            })
            .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.GetUpcomingStats(_ct);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(stats);

        capturedStart.Should().NotBeNull();
        capturedEnd.Should().NotBeNull();
        capturedEnd!.Value.Should().Be(capturedStart!.Value.AddDays(7));

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_WhenServiceReturnsFailure_ReturnsStatusCodeFromResolverWithErrors()
    {
        // Arrange
        var dto = _fixture.Create<UpsertFlightScheduleDto>();

        var errors = new List<Error> { new() { Type = ErrorType.Validation, Message = "invalid" } };
        var serviceResult = new Result<UpsertFlightScheduleResultDto>(errors);

        _flightSchedulesService.Setup(s => s.CreateAsync(dto, _ct))
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
    public async Task Create_WhenServiceReturnsSuccess_ReturnsCreatedAtActionWithIdFromResult()
    {
        // Arrange
        var dto = _fixture.Create<UpsertFlightScheduleDto>();
        var id = _fixture.Create<int>();

        var scheduleDto = _fixture.Build<GetFlightScheduleDto>()
                                  .With(x => x.FlightScheduleId, id)
                                  .Create();

        var resultDto = new UpsertFlightScheduleResultDto
        {
            FlightSchedule = scheduleDto,
            ScheduleConflicts = Array.Empty<ScheduleConflictDto>()
        };

        var serviceResult = new Result<UpsertFlightScheduleResultDto> { Value = resultDto };

        _flightSchedulesService.Setup(s => s.CreateAsync(dto, _ct))
                               .ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _sut.Create(dto, _ct);

        // Assert
        var createdAt = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdAt.ActionName.Should().Be(nameof(FlightSchedulesController.GetById));
        createdAt.Value.Should().BeSameAs(resultDto);

        createdAt.RouteValues.Should().NotBeNull();
        createdAt.RouteValues!["id"].Should().Be(id);

        _severityResolver.Verify(r => r.GetHttpStatusCode(It.IsAny<Result>()), Times.Never);
    }

    #endregion

    #region Import

    [Fact]
    public async Task Import_WhenFileIsNull_ReturnsBadRequestWithValidationError()
    {
        // Arrange
        var request = new ImportSchedulesRequest { File = null };

        // Act
        var actionResult = await _sut.Import(request, _ct);

        // Assert
        var bad = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errors = bad.Value.Should().BeAssignableTo<List<Error>>().Subject;

        errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == "File is required.");

        _flightSchedulesService.Verify(
            s => s.ImportAsync(It.IsAny<IEnumerable<UpsertFlightScheduleDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Import_WhenFileIsEmpty_ReturnsBadRequestWithValidationError()
    {
        // Arrange
        var file = CreateFormFileMock(fileName: "schedules.json", length: 0, jsonContent: "[]").Object;
        var request = new ImportSchedulesRequest { File = file };

        // Act
        var actionResult = await _sut.Import(request, _ct);

        // Assert
        var bad = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errors = bad.Value.Should().BeAssignableTo<List<Error>>().Subject;

        errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == "File is required.");
    }

    [Fact]
    public async Task Import_WhenFileTooLargeAndWrongExtension_ReturnsBadRequestWithMultipleValidationErrors()
    {
        // Arrange
        const long maxBytes = 2 * 1024 * 1024;
        var file = CreateFormFileMock(fileName: "schedules.txt", length: maxBytes + 1, jsonContent: "[]").Object;
        var request = new ImportSchedulesRequest { File = file };

        // Act
        var actionResult = await _sut.Import(request, _ct);

        // Assert
        var bad = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errors = bad.Value.Should().BeAssignableTo<List<Error>>().Subject;

        errors.Should().Contain(e => e.Type == ErrorType.Validation && e.Message == "File size must not exceed 2 MB.");
        errors.Should().Contain(e => e.Type == ErrorType.Validation && e.Message == "Invalid file type. Please upload a .json file.");

        _flightSchedulesService.Verify(
            s => s.ImportAsync(It.IsAny<IEnumerable<UpsertFlightScheduleDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Import_WhenJsonCannotBeParsed_ReturnsBadRequestWithParseError()
    {
        // Arrange
        var file = CreateFormFileMock(fileName: "schedules.json", length: 10, jsonContent: "{ this is not json }").Object;
        var request = new ImportSchedulesRequest { File = file };

        // Act
        var actionResult = await _sut.Import(request, _ct);

        // Assert
        var bad = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errors = bad.Value.Should().BeAssignableTo<List<Error>>().Subject;

        errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == "Invalid JSON file. Could not parse content.");

        _flightSchedulesService.Verify(
            s => s.ImportAsync(It.IsAny<IEnumerable<UpsertFlightScheduleDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Import_WhenJsonIsEmptyArray_ReturnsBadRequestWithValidationError()
    {
        // Arrange
        var file = CreateFormFileMock(fileName: "schedules.json", length: 2, jsonContent: "[]").Object;
        var request = new ImportSchedulesRequest { File = file };

        // Act
        var actionResult = await _sut.Import(request, _ct);

        // Assert
        var bad = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errors = bad.Value.Should().BeAssignableTo<List<Error>>().Subject;

        errors.Should().ContainSingle(e =>
            e.Type == ErrorType.Validation &&
            e.Message == "Invalid JSON content. Expected a non-empty array of schedules.");

        _flightSchedulesService.Verify(
            s => s.ImportAsync(It.IsAny<IEnumerable<UpsertFlightScheduleDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Import_WhenAllRowsCreated_Returns201CreatedWithSummary()
    {
        // Arrange
        var rows = _fixture.CreateMany<UpsertFlightScheduleDto>(2).ToList();
        var json = JsonSerializer.Serialize(rows);

        var file = CreateFormFileMock(fileName: "schedules.json", length: 10, jsonContent: json).Object;
        var request = new ImportSchedulesRequest { File = file };

        var summary = new ImportSummaryDto
        {
            Total = rows.Count,
            Created = rows.Count,
            Updated = 0,
            Failed = 0,
            Errors = new List<ImportRowErrorDto>()
        };

        _flightSchedulesService.Setup(s => s.ImportAsync(It.IsAny<IEnumerable<UpsertFlightScheduleDto>>(), _ct))
                               .ReturnsAsync(summary);

        // Act
        var actionResult = await _sut.Import(request, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status201Created);
        obj.Value.Should().BeSameAs(summary);
    }

    [Fact]
    public async Task Import_WhenSomeRowsFail_Returns207MultiStatusWithSummary()
    {
        // Arrange
        var rows = _fixture.CreateMany<UpsertFlightScheduleDto>(2).ToList();
        var json = JsonSerializer.Serialize(rows);

        var file = CreateFormFileMock(fileName: "schedules.json", length: 10, jsonContent: json).Object;
        var request = new ImportSchedulesRequest { File = file };

        var summary = new ImportSummaryDto
        {
            Total = rows.Count,
            Created = 1,
            Updated = 0,
            Failed = 1,
            Errors = new List<ImportRowErrorDto>
            {
                new() { Row = 2, Message = "Gate overlap" }
            }
        };

        _flightSchedulesService.Setup(s => s.ImportAsync(It.IsAny<IEnumerable<UpsertFlightScheduleDto>>(), _ct))
                               .ReturnsAsync(summary);

        // Act
        var actionResult = await _sut.Import(request, _ct);

        // Assert
        var obj = actionResult.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status207MultiStatus);
        obj.Value.Should().BeSameAs(summary);
    }

    #endregion

    #region Test helpers

    private static Mock<IFormFile> CreateFormFileMock(string fileName, long length, string jsonContent)
    {
        var bytes = Encoding.UTF8.GetBytes(jsonContent);

        Stream OpenStream() => new MemoryStream(bytes);

        var formFile = new Mock<IFormFile>(MockBehavior.Strict);
        formFile.SetupGet(f => f.FileName).Returns(fileName);
        formFile.SetupGet(f => f.Length).Returns(length);
        formFile.Setup(f => f.OpenReadStream()).Returns(OpenStream);

        return formFile;
    }

    #endregion
}
