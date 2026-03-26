using AirportTool.Application;
using Microsoft.AspNetCore.Mvc;

namespace AirportTool.WebApi;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IResultSeverityResolver _severityResolver;

    public BookingsController(
        IBookingService bookingService,
        IResultSeverityResolver severityResolver)
    {
        _bookingService = bookingService;
        _severityResolver = severityResolver;
    }

    [HttpGet("{code}")]
    [ProducesResponseType(typeof(GetBookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status500InternalServerError)]

    public async Task<IActionResult> GetByCode([FromRoute] string code, CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetBookingByCodeAsync(code, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status500InternalServerError)]

    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CreateBookingAsync(dto, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        var confirmationCode = result.Value!.ConfirmationCode;
        return CreatedAtAction(nameof(GetByCode), new { code = confirmationCode }, result.Value);
    }

    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(List<Error>), StatusCodes.Status500InternalServerError)]

    public async Task<IActionResult> Cancel([FromRoute] string code, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CancelBookingAsync(code, cancellationToken);

        if (result.IsFailure)
        {
            var status = _severityResolver.GetHttpStatusCode(result);
            return StatusCode((int)status, result.Errors);
        }

        return NoContent();
    }
}
