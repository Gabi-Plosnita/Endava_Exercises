using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class CreateBookingDto
{
    [Range(1, long.MaxValue, ErrorMessage = "TicketId must be a positive value.")]
    public long TicketId { get; set; }

    [Required(ErrorMessage = "PassengerFullName is required.")]
    public string PassengerFullName { get; set; } = null!;

    [Required(ErrorMessage = "PassengerEmail is required.")]
    [EmailAddress(ErrorMessage = "PassengerEmail is not a valid email address.")]
    public string PassengerEmail { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer.")]
    public int Quantity { get; set; }
}
