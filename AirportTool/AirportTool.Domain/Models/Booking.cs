namespace AirportTool.Domain;

public class Booking
{
    public long Id { get; set; }

    public long TicketId { get; set; }

    public string PassengerFullName { get; set; } = null!;

    public string PassengerEmail { get; set; } = null!;

    public string ConfirmationCode { get; set; } = null!;

    public int Quantity { get; set; }

    public BookingStatus Status { get; set; } 

    public DateTime CreatedUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
