using AirportTool.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirportTool.Application;

public class GetTicketDto
{
    public long TicketId { get; set; }

    public int FlightScheduleId { get; set; }

    public FareClass FareClass { get; set; }

    public decimal BasePrice { get; set; }

    public decimal Taxes { get; set; }

    public decimal? TotalPrice { get; set; }

    public string Currency { get; set; } = null!;

    public bool IsRefundable { get; set; }

    public int SeatInventory { get; set; }
}
