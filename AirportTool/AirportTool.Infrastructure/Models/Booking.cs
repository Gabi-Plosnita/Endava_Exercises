using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirportTool.Infrastructure;

[Table("Booking")]
[Index("TicketId", Name = "IX_Booking_TicketId")]
[Index("ConfirmationCode", Name = "UQ__Booking__196830863801910F", IsUnique = true)]
public partial class Booking
{
    [Key]
    public long BookingId { get; set; }

    public long TicketId { get; set; }

    [StringLength(120)]
    public string PassengerFullName { get; set; } = null!;

    [StringLength(120)]
    public string PassengerEmail { get; set; } = null!;

    [StringLength(8)]
    public string ConfirmationCode { get; set; } = null!;

    public int Quantity { get; set; }

    public byte Status { get; set; }

    public DateTime CreatedUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    [ForeignKey("TicketId")]
    [InverseProperty("Bookings")]
    public virtual Ticket Ticket { get; set; } = null!;
}
