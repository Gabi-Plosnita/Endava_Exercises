namespace AirportTool.Application;

public class GetGateDto
{
    public int GateId { get; set; }

    public string AirportIataCode { get; set; } = null!;

    public string AirportName { get; set; } = null!;

    public string Code { get; set; } = null!;
}
