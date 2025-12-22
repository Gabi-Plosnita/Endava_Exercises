using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class CreateGateDto
{
    [Required(ErrorMessage = "AirportIataCode is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "AirportIataCode must be exactly 3 characters.")]
    public string AirportIataCode { get; set; } = null!;

    [Required(ErrorMessage = "Code is required.")]
    public string Code { get; set; } = null!;
}
