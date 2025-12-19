namespace AirportTool.Application;

public class UpdateGateDto
{
    public string AirportIataCode { get; set; } = null!;

    public string GateCode { get; set; } = null!;
}
