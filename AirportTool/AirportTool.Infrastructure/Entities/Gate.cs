using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirportTool.Infrastructure;

[Table("Gate")]
[Index("AirportId", "Code", Name = "UQ__Gate__99FE25417E8B484B", IsUnique = true)]
public partial class Gate
{
    [Key]
    public int GateId { get; set; }

    public int AirportId { get; set; }

    [StringLength(10)]
    public string Code { get; set; } = null!;

    [ForeignKey("AirportId")]
    [InverseProperty("Gates")]
    public virtual Airport Airport { get; set; } = null!;

    [InverseProperty("Gate")]
    public virtual ICollection<FlightSchedule> FlightSchedules { get; set; } = new List<FlightSchedule>();
}
