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

    public async Task<Flight?> GetFlightByAirlineAndFlightNumberAsync(
        string airlineIataCode, string flightNumber, CancellationToken cancellationToken = default)
    {
        var flightDb = await _context.Flights
                                     .AsNoTracking()
                                     .Where(f => f.Airline.Iatacode == airlineIataCode && f.FlightNumber == flightNumber)
                                     .SingleOrDefaultAsync(cancellationToken);

        return _mapper.Map<Flight>(flightDb);
    }

    public async Task<Flight> GetByAirlineIdAndFlightNumberAsync(
        int airlineId, string flightNumber, CancellationToken cancellationToken = default)
    {
        var flightDb = await _context.Flights
                                     .AsNoTracking()
                                     .Where(f => f.AirlineId == airlineId && f.FlightNumber == flightNumber)
                                     .SingleOrDefaultAsync(cancellationToken);

        return _mapper.Map<Flight>(flightDb);
    }

    public async Task<bool> HasAnyFlightSchedulesAsync(int flightId, CancellationToken cancellationToken = default)
    {
        return await _context.FlightSchedules
                             .AsNoTracking()
                             .AnyAsync(fs => fs.FlightId == flightId, cancellationToken);
    }
}
