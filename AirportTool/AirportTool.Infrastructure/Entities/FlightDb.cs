using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirportTool.Infrastructure;

[Table("Flight")]
[Index("AirlineId", "FlightNumber", Name = "IX_Flight_Airline_FlightNumber")]
[Index("OriginAirportId", "DestinationAirportId", Name = "IX_Flight_Origin_Destination")]
public partial class FlightDb
{
    [Key]
    public int FlightId { get; set; }

    public int AirlineId { get; set; }

    [StringLength(8)]
    public string FlightNumber { get; set; } = null!;

    public int OriginAirportId { get; set; }

    public int DestinationAirportId { get; set; }

    public int? DefaultAircraftId { get; set; }

    public bool IsActive { get; set; }

    [ForeignKey("AirlineId")]
    [InverseProperty("Flights")]
    public virtual AirlineDb Airline { get; set; } = null!;

    [ForeignKey("DefaultAircraftId")]
    [InverseProperty("Flights")]
    public virtual AircraftDb? DefaultAircraft { get; set; }

    [ForeignKey("DestinationAirportId")]
    [InverseProperty("FlightDestinationAirports")]
    public virtual AirportDb DestinationAirport { get; set; } = null!;

    [InverseProperty("Flight")]
    public virtual ICollection<FlightScheduleDb> FlightSchedules { get; set; } = new List<FlightScheduleDb>();

    [ForeignKey("OriginAirportId")]
    [InverseProperty("FlightOriginAirports")]
    public virtual AirportDb OriginAirport { get; set; } = null!;
}
