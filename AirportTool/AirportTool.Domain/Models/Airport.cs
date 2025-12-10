namespace AirportTool.Domain;

public class Airport
{
    public int Id { get; set; }

    public string Iatacode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? City { get; set; }

    public string? Country { get; set; }

    public string TimeZone { get; set; } = null!;
}
