using AirportTool.Application;
using Microsoft.AspNetCore.Mvc;

namespace AirportTool.WebApi;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IResultSeverityResolver _severityResolver;

    public TicketsController(
        ITicketService ticketService,
        IResultSeverityResolver severityResolver)
    {
        _ticketService = ticketService;
        _severityResolver = severityResolver;
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(GetTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken cancellationToken)
    {
        var result = await _ticketService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet("by-flightSchedule/{flightScheduleId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<GetTicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByFlightSchedule([FromRoute] int flightScheduleId, CancellationToken cancellationToken)
    {
        var result = await _ticketService.GetByFlightScheduleIdAsync(flightScheduleId, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GetTicketDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateTicketDto dto, CancellationToken cancellationToken)
    {
        var result = await _ticketService.CreateAsync(dto, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.TicketId }, result.Value);
    }

    [HttpPut("{id:long}/inventory")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInventory([FromRoute] long id, [FromBody] UpdateTicketDto dto, CancellationToken cancellationToken)
    {
        var result = await _ticketService.UpdateAsync(id, dto, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken cancellationToken)
    {
        var result = await _ticketService.DeleteByIdAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return NoContent();
    }
}
