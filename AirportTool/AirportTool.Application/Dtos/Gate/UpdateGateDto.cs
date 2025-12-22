using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class UpdateGateDto
{
    [Required(ErrorMessage = "GateCode is required.")]
    public string GateCode { get; set; } = null!;
}
