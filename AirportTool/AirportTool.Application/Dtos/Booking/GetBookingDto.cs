using AirportTool.Domain;

namespace AirportTool.Application;

public class GetBookingDto
{
    public long BookingId { get; set; }

    public long TicketId { get; set; }

    public string PassengerFullName { get; set; } = null!;

    public string PassengerEmail { get; set; } = null!;

    public string ConfirmationCode { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; }
}
