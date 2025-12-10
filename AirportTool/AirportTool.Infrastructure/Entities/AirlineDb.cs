using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirportTool.Infrastructure;

[Table("Airline")]
[Index("Iatacode", Name = "UQ__Airline__EFD6F5BEE63D3A0C", IsUnique = true)]
public partial class AirlineDb
{
    [Key]
    public int AirlineId { get; set; }

    [Column("IATACode")]
    [StringLength(2)]
    public string Iatacode { get; set; } = null!;

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [InverseProperty("OwnedByAirline")]
    public virtual ICollection<AircraftDb> Aircraft { get; set; } = new List<AircraftDb>();

    [InverseProperty("Airline")]
    public virtual ICollection<FlightDb> Flights { get; set; } = new List<FlightDb>();
}
