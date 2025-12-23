using AirportTool.Application;
using AirportTool.Domain;
using Microsoft.AspNetCore.Mvc;

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
        dto.Status = FlightScheduleStatus.Planned;
        var result = await _flightSchedulesService.CreateAsync(dto, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.FlightSchedule!.FlightScheduleId }, result.Value);
    }
}
