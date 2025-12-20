namespace AirportTool.Application;

public class CreateBookingDto
{
    public long TicketId { get; set; }

    public string PassengerFullName { get; set; } = null!;

    public string PassengerEmail { get; set; } = null!;

    public int Quantity { get; set; }
}
