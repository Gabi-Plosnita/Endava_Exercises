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

    public async Task<FlightSchedule?> GetByFlightAndDepartureAsync(
        int flightId, DateTime departureUtc, CancellationToken cancellationToken)
    {
        var flightScheduleDb = await _context.FlightSchedules
                                             .FirstOrDefaultAsync(fs => fs.FlightId == flightId
                                                                  && fs.ScheduledDepartureUtc == departureUtc,
                                                                  cancellationToken);

        return _mapper.Map<FlightSchedule>(flightScheduleDb);
    }

    public Task<GetFlightScheduleDto?> GetDtoByIdAsync(
        int flightScheduleId, CancellationToken cancellationToken)
    {
        return _context.FlightSchedules
                       .AsNoTracking()
                       .Where(fs => fs.FlightScheduleId == flightScheduleId)
                       .Select(fs => new GetFlightScheduleDto
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

    public async Task<PagedResult<FlightScheduleSearchDto>> GetFilteredFlightSchedulesAsync(
        FlightScheduleFilterDto filter, CancellationToken cancellationToken)
    {
        var skip = filter.PageIndex * filter.PageSize;

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

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query.OrderBy(fs => fs.ScheduledDepartureUtc)
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
                               })
                               .ToListAsync(cancellationToken);

        return new PagedResult<FlightScheduleSearchDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<IReadOnlyList<DailyFlightStatsDto>> GetDailyStatsAsync(
        DateOnly startUtc, DateOnly endUtc, CancellationToken cancellationToken)
    {
        var startDateTime = startUtc.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endUtc.ToDateTime(TimeOnly.MaxValue);

        var flightScheduleDbs = await _context.FlightSchedules
                                              .AsNoTracking()
                                              .Where(fs => fs.ScheduledDepartureUtc >= startDateTime 
                                                           && fs.ScheduledDepartureUtc <= endDateTime)
                                              .ToListAsync(cancellationToken);

        var groupedFlightSchedules = flightScheduleDbs.GroupBy(fs => DateOnly.FromDateTime(fs.ScheduledDepartureUtc))
                                                      .ToDictionary(g => g.Key, g => g.Count());

        var results = new List<DailyFlightStatsDto>();
        var currentDate = startUtc;

        while (currentDate <= endUtc)
        {
            groupedFlightSchedules.TryGetValue(currentDate, out var count);

            results.Add(new DailyFlightStatsDto
            {
                Date = currentDate,
                TotalFlights = count
            });

            currentDate = currentDate.AddDays(1);
        }

        return results;
    }

    public async Task AddAndSaveAsync(FlightSchedule flightSchedule, CancellationToken cancellationToken)
    {
        var flightScheduleDb = _mapper.Map<FlightScheduleDb>(flightSchedule);
        _context.FlightSchedules.Add(flightScheduleDb);
        await _context.SaveChangesAsync(cancellationToken);
        _mapper.Map(flightScheduleDb, flightSchedule);
    }

    public async Task<IReadOnlyList<ScheduleConflictDto>> GetGateOverlapsAsync(
        int? gateId,
        DateTime proposedStartUtc,
        DateTime proposedEndUtc,
        int? excludeFlightScheduleId,
        CancellationToken cancellationToken)
    {
        var conflicts = await _context.FlightSchedules
            .AsNoTracking()
            .Where(fs => fs.GateId == gateId
                         && (excludeFlightScheduleId == null || fs.FlightScheduleId != excludeFlightScheduleId))
            .Where(fs => proposedStartUtc < fs.ScheduledArrivalUtc && fs.ScheduledDepartureUtc < proposedEndUtc)
            .OrderBy(fs => fs.ScheduledDepartureUtc)
            .Select(fs => new ScheduleConflictDto
            {
                FlightScheduleId = fs.FlightScheduleId,
                FlightNumber = fs.Flight.FlightNumber,
                AirlineIata = fs.Flight.Airline.Iatacode,
                ScheduledDepartureUtc = fs.ScheduledDepartureUtc,
                ScheduledArrivalUtc = fs.ScheduledArrivalUtc
            })
            .ToListAsync(cancellationToken);

        return conflicts;
    }
}
