using AirportTool.Domain;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirportTool.Infrastructure;

[Table("FlightSchedule")]
[Index("FlightId", "ScheduledDepartureUtc", Name = "IX_FlightSchedule_Flight_Departure")]
public partial class FlightScheduleDb
{
    [Key]
    public int FlightScheduleId { get; set; }

    public int FlightId { get; set; }

    public DateTime ScheduledDepartureUtc { get; set; }

    public DateTime ScheduledArrivalUtc { get; set; }

    public int? GateId { get; set; }

    public int? AssignedAircraftId { get; set; }

    public FlightScheduleStatus Status { get; set; } 

    [ForeignKey("AssignedAircraftId")]
    [InverseProperty("FlightSchedules")]
    public virtual AircraftDb? AssignedAircraft { get; set; }

    [ForeignKey("FlightId")]
    [InverseProperty("FlightSchedules")]
    public virtual FlightDb Flight { get; set; } = null!;

    [ForeignKey("GateId")]
    [InverseProperty("FlightSchedules")]
    public virtual GateDb? Gate { get; set; }

    [InverseProperty("FlightSchedule")]
    public virtual ICollection<TicketDb> Tickets { get; set; } = new List<TicketDb>();
}
