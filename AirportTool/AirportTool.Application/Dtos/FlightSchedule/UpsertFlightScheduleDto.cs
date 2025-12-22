using AirportTool.Domain;
using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class UpsertFlightScheduleDto : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "FlightId must be a positive value.")]
    public int FlightId { get; set; }

    [Required(ErrorMessage = "ScheduledDepartureUtc is required.")]
    public DateTime ScheduledDepartureUtc { get; set; }

    [Required(ErrorMessage = "ScheduledArrivalUtc is required.")]
    public DateTime ScheduledArrivalUtc { get; set; }

    public string? GateCode { get; set; }

    public string? AssignedAircraftTail { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public FlightScheduleStatus Status { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ScheduledArrivalUtc <= ScheduledDepartureUtc)
        {
            yield return new ValidationResult(
                "ScheduledArrivalUtc must be later than ScheduledDepartureUtc.",
                new[] { nameof(ScheduledArrivalUtc), nameof(ScheduledDepartureUtc) }
            );

            yield break;
        }

        if (ScheduledArrivalUtc - ScheduledDepartureUtc < TimeSpan.FromMinutes(10))
        {
            yield return new ValidationResult(
                "The duration between ScheduledDepartureUtc and ScheduledArrivalUtc cannot be less than 10 minutes.",
                new[] { nameof(ScheduledArrivalUtc), nameof(ScheduledDepartureUtc) }
            );
        }
    }
}
