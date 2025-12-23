using AirportTool.Application;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AirportTool.WebApi;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly IFlightSchedulesService _flightSchedulesService;
    private readonly IResultSeverityResolver _severityResolver;

    public SchedulesController(
        IFlightSchedulesService flightSchedulesService,
        IResultSeverityResolver severityResolver)
    {
        _flightSchedulesService = flightSchedulesService;
        _severityResolver = severityResolver;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetFlightScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _flightSchedulesService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FlightScheduleSearchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByFilter([FromQuery] FlightScheduleFilterDto dto, CancellationToken cancellationToken)
    {
        var result = await _flightSchedulesService.GetByFilterAsync(dto, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet("stats/upcoming")]
    [ProducesResponseType(typeof(IReadOnlyList<DailyFlightStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUpcomingStats(CancellationToken cancellationToken)
    {
        var startUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var endUtc = startUtc.AddDays(7);

        var result = await _flightSchedulesService.GetDailyStatsAsync(startUtc, endUtc, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UpsertFlightScheduleResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] UpsertFlightScheduleDto dto, CancellationToken cancellationToken)
    {
        var result = await _flightSchedulesService.CreateAsync(dto, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.FlightSchedule!.FlightScheduleId }, result.Value);
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ImportSummaryDto), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import([FromForm] ImportSchedulesRequest request, CancellationToken cancellationToken)
    {
        var file = request.File;

        if (file is null || file.Length == 0)
        {
            return BadRequest(new List<Error>
            {
                new Error { Message = "File is required.", Type = ErrorType.Validation }
            });
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new List<Error>
            {
                new Error { Message = "Invalid file type. Please upload a .json file.", Type = ErrorType.Validation }
            });
        }

        List<UpsertFlightScheduleDto>? rows;
        try
        {
            await using var stream = file.OpenReadStream();
            rows = await JsonSerializer.DeserializeAsync<List<UpsertFlightScheduleDto>>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (rows is null || rows.Count == 0)
            {
                return BadRequest(new List<Error>
                {
                    new Error { Message = "Invalid JSON content. Expected a non-empty array of schedules.", Type = ErrorType.Validation }
                });
            }
        }
        catch (JsonException)
        {
            return BadRequest(new List<Error>
            {
                new Error { Message = "Invalid JSON file. Could not parse content.", Type = ErrorType.Validation }
            });
        }

        var summary = await _flightSchedulesService.ImportAsync(rows, cancellationToken);

        var allCreated =
            summary.Total > 0 &&
            summary.Created == summary.Total &&
            summary.Updated == 0 &&
            summary.Failed == 0 &&
            (summary.Errors?.Count ?? 0) == 0;

        return allCreated
            ? StatusCode(StatusCodes.Status201Created, summary)
            : StatusCode(StatusCodes.Status207MultiStatus, summary);
    }

}
