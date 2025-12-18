namespace AirportTool.Application;

public class CreateGateDto
{
    public string AirportIataCode { get; set; } = null!;

    public string Code { get; set; } = null!;
}
