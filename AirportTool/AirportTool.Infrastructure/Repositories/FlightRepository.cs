using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public class FlightRepository : EfRepositoryBase<Flight, FlightDb, int>, IFlightRepository
{
    public FlightRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }

    public async Task<IReadOnlyCollection<FlightSearchDto>> GetFilteredFlightsAsync(
        FlightFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var skip = (filter.PageIndex - 1) * filter.PageSize;

        var query = _context.FlightSchedules
                            .AsNoTracking()
                            .Include(fs => fs.Flight).ThenInclude(f => f.Airline)
                            .Include(fs => fs.Flight).ThenInclude(f => f.OriginAirport)
                            .Include(fs => fs.Flight).ThenInclude(f => f.DestinationAirport)
                            .Include(fs => fs.Flight).ThenInclude(f => f.DefaultAircraft)
                            .Include(fs => fs.AssignedAircraft)
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
                                  .Select(fs => new FlightSearchDto
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

                                      AircraftTail = fs.AssignedAircraft != null
                                                        ? fs.AssignedAircraft.TailNumber
                                                        : fs.Flight.DefaultAircraft != null
                                                            ? fs.Flight.DefaultAircraft.TailNumber
                                                            : null
                                  });

        var results = await projectedQuery.ToListAsync(cancellationToken);

        return results;
    }
}