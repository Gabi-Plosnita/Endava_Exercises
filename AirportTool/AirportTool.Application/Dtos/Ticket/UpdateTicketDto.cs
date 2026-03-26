using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class UpdateTicketDto
{
    [Range(0, int.MaxValue, ErrorMessage = "Seat inventory cannot be negative.")]
    public int SeatInventory { get; set; }
}
