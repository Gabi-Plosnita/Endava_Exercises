using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public class FlightScheduleRepository : EfRepositoryBase<FlightSchedule, FlightScheduleDb, int>, IFlightScheduleRepository
{
    public FlightScheduleRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }

    public Task<FlightScheduleDetailsDto?> GetFlightScheduleDetailsAsync(
        int flightScheduleId, CancellationToken cancellationToken = default)
    {
        return _context.FlightSchedules
                       .AsNoTracking()
                       .Where(fs => fs.FlightScheduleId == flightScheduleId)
                       .Select(fs => new FlightScheduleDetailsDto
                       {
                           FlightScheduleId = fs.FlightScheduleId,
                           FlightId = fs.FlightId,
                           FlightNumber = fs.Flight.FlightNumber,
                           AirlineIata = fs.Flight.Airline.Iatacode,
                           AirlineName = fs.Flight.Airline.Name,
                           OriginIata = fs.Flight.OriginAirport.Iatacode,
                           OriginName = fs.Flight.OriginAirport.Name,
                           DestinationIata = fs.Flight.DestinationAirport.Iatacode,
                           DestinationName = fs.Flight.DestinationAirport.Name,
                           ScheduledDepartureUtc = fs.ScheduledDepartureUtc,
                           ScheduledArrivalUtc = fs.ScheduledArrivalUtc,
                           Status = fs.Status,
                           AssignedAircraftTail = fs.AssignedAircraft != null
                                                   ? fs.AssignedAircraft.TailNumber
                                                   : null,
                           DefaultAircraftTail = fs.Flight.DefaultAircraft != null
                                                   ? fs.Flight.DefaultAircraft.TailNumber
                                                   : null,
                           GateCode = fs.Gate != null ? fs.Gate.Code : null
                       })
                       .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FlightScheduleSearchDto>> GetFilteredFlightSchedulesAsync(
        FlightFilterDto filter, CancellationToken cancellationToken = default)
    {
        var skip = (filter.PageIndex - 1) * filter.PageSize;

        var query = _context.FlightSchedules
                            .AsNoTracking()
                            .Where(fs => fs.Flight.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.OriginIata))
        {
            query = query.Where(fs => fs.Flight.OriginAirport.Iatacode == filter.OriginIata);
        }

        if (!string.IsNullOrWhiteSpace(filter.DestinationIata))
        {
            query = query.Where(fs => fs.Flight.DestinationAirport.Iatacode == filter.DestinationIata);
        }

        if (filter.Date.HasValue)
        {
            var dateStart = filter.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dateEnd = dateStart.AddDays(1);

            query = query.Where(fs =>
                fs.ScheduledDepartureUtc >= dateStart &&
                fs.ScheduledDepartureUtc < dateEnd);
        }

        var projectedQuery = query.OrderBy(fs => fs.ScheduledDepartureUtc)
                                  .Skip(skip)
                                  .Take(filter.PageSize)
                                  .Select(fs => new FlightScheduleSearchDto
                                  {
                                      FlightScheduleId = fs.FlightScheduleId,
                                      FlightId = fs.FlightId,
                                      FlightNumber = fs.Flight.FlightNumber,

                                      AirlineIata = fs.Flight.Airline.Iatacode,
                                      AirlineName = fs.Flight.Airline.Name,

                                      OriginIata = fs.Flight.OriginAirport.Iatacode,
                                      OriginName = fs.Flight.OriginAirport.Name,

                                      DestinationIata = fs.Flight.DestinationAirport.Iatacode,
                                      DestinationName = fs.Flight.DestinationAirport.Name,

                                      ScheduledDepartureUtc = fs.ScheduledDepartureUtc,
                                      ScheduledArrivalUtc = fs.ScheduledArrivalUtc,

                                      Status = fs.Status,

                                      DefaultAircraftTail = fs.Flight.DefaultAircraft != null
                                                                ? fs.Flight.DefaultAircraft.TailNumber
                                                                : null,

                                      AssignedAircraftTail = fs.AssignedAircraft != null
                                                                ? fs.AssignedAircraft.TailNumber
                                                                : null
                                  });

        var results = await projectedQuery.ToListAsync(cancellationToken);

        return results;
    }
}
