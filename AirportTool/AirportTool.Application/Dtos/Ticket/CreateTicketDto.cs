using AirportTool.Domain;
using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class CreateTicketDto
{
    [Range(1, int.MaxValue, ErrorMessage = "FlightScheduleId must be a positive value.")]
    public int FlightScheduleId { get; set; }

    [Required(ErrorMessage = "FareClass is required.")]
    public FareClass FareClass { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "BasePrice must be a positive value.")]
    public decimal BasePrice { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Taxes must be a positive value.")]
    public decimal Taxes { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a valid 3-letter ISO currency code.")]
    public string Currency { get; set; } = null!;

    public bool IsRefundable { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SeatInventory must be a positive value.")]
    public int SeatInventory { get; set; }
}
