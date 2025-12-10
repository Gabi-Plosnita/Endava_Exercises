using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirportTool.Infrastructure;

[Table("Airport")]
[Index("Iatacode", Name = "UQ__Airport__EFD6F5BE6A869E5A", IsUnique = true)]
public partial class AirportDb
{
    [Key]
    public int AirportId { get; set; }

    [Column("IATACode")]
    [StringLength(3)]
    public string Iatacode { get; set; } = null!;

    [StringLength(120)]
    public string Name { get; set; } = null!;

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(80)]
    public string? Country { get; set; }

    [StringLength(64)]
    public string TimeZone { get; set; } = null!;

    [InverseProperty("DestinationAirport")]
    public virtual ICollection<FlightDb> FlightDestinationAirports { get; set; } = new List<FlightDb>();

    [InverseProperty("OriginAirport")]
    public virtual ICollection<FlightDb> FlightOriginAirports { get; set; } = new List<FlightDb>();

    [InverseProperty("Airport")]
    public virtual ICollection<GateDb> Gates { get; set; } = new List<GateDb>();
}
