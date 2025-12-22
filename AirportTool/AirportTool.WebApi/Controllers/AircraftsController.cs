using AirportTool.Application;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AirportTool.WebApi;

[ApiController]
[Route("api/[controller]")]
public class AircraftsController : ControllerBase
{
    private readonly IAircraftService _aircraftService;
    private readonly IResultSeverityResolver _severityResolver;

    public AircraftsController(
        IAircraftService aircraftService,
        IResultSeverityResolver severityResolver)
    {
        _aircraftService = aircraftService;
        _severityResolver = severityResolver;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetAircraftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _aircraftService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GetAircraftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByFilter([FromQuery] AircraftFilterDto dto, CancellationToken cancellationToken)
    {
        var result = await _aircraftService.GetByFilterAsync(dto, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GetAircraftDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAircraftDto dto, CancellationToken cancellationToken)
    {
        var result = await _aircraftService.CreateAsync(dto, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        if (result.Value == null)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new List<Error>
            {
                new Error { Message = "Aircraft was created but could not be retrieved.", Type = ErrorType.Unexpected }
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.AircraftId }, result.Value);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAircraftDto dto, CancellationToken cancellationToken)
    {
        var result = await _aircraftService.UpdateAsync(id, dto, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _aircraftService.DeleteByIdAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return NoContent();
    }
}
