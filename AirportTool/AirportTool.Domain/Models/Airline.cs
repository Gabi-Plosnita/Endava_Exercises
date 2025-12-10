namespace AirportTool.Domain;

public class Airline
{
    public int Id { get; set; }

    public string Iatacode { get; set; } = null!;

    public string Name { get; set; } = null!;
}
