namespace AirportTool.Domain;

public class Gate
{
    public int GateId { get; set; }

    public int AirportId { get; set; }

    public string GateCode { get; set; } = null!;
}
