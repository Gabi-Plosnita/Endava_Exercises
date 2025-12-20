using AirportTool.Domain;

namespace AirportTool.Application;

public class GetBookingDto
{
    public long TicketId { get; set; }

    public string PassengerFullName { get; set; } = null!;

    public string PassengerEmail { get; set; } = null!;

    public string ConfirmationCode { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal TotalAmount { get; set; }

    public BookingStatus Status { get; set; }
}
