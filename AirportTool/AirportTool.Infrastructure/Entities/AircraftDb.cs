using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirportTool.Infrastructure;

[Index("TailNumber", Name = "UQ__Aircraft__3F41D11BCE3D7A41", IsUnique = true)]
public partial class AircraftDb
{
    [Key]
    public int AircraftId { get; set; }

    [StringLength(10)]
    public string TailNumber { get; set; } = null!;

    [StringLength(60)]
    public string Model { get; set; } = null!;

    public int SeatCapacity { get; set; }

    public int? OwnedByAirlineId { get; set; }

    [InverseProperty("AssignedAircraft")]
    public virtual ICollection<FlightScheduleDb> FlightSchedules { get; set; } = new List<FlightScheduleDb>();

    [InverseProperty("DefaultAircraft")]
    public virtual ICollection<FlightDb> Flights { get; set; } = new List<FlightDb>();

    [ForeignKey("OwnedByAirlineId")]
    [InverseProperty("Aircraft")]
    public virtual AirlineDb? OwnedByAirline { get; set; }
}
