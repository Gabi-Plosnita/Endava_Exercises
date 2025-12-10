using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirportTool.Infrastructure;

[Index("TailNumber", Name = "UQ__Aircraft__3F41D11BCE3D7A41", IsUnique = true)]
public partial class Aircraft
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
    public virtual ICollection<FlightSchedule> FlightSchedules { get; set; } = new List<FlightSchedule>();

    [InverseProperty("DefaultAircraft")]
    public virtual ICollection<Flight> Flights { get; set; } = new List<Flight>();

    [ForeignKey("OwnedByAirlineId")]
    [InverseProperty("Aircraft")]
    public virtual Airline? OwnedByAirline { get; set; }
}
