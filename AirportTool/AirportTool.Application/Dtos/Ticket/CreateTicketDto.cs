using AirportTool.Domain;

namespace AirportTool.Application;

public class CreateTicketDto
{
    public int FlightScheduleId { get; set; }

    public FareClass FareClass { get; set; }

    public decimal BasePrice { get; set; }

    public decimal Taxes { get; set; }

    public string Currency { get; set; } = null!;

    public bool IsRefundable { get; set; }

    public int SeatInventory { get; set; }
}
