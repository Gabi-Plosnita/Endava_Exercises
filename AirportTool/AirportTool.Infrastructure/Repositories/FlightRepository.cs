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

    public Task<GetFlightDto?> GetFlightDtoByIdAsync(int flightId, CancellationToken cancellationToken)
    {
        return _context.Flights
                       .AsNoTracking()
                       .Where(f => f.FlightId == flightId)
                       .Select(f => new GetFlightDto
                       {
                           FlightId = f.FlightId,
                           FlightNumber = f.FlightNumber,
                           AirlineIata = f.Airline.Iatacode,
                           AirlineName = f.Airline.Name,
                           OriginIata = f.OriginAirport.Iatacode,
                           OriginName = f.OriginAirport.Name,
                           DestinationIata = f.DestinationAirport.Iatacode,
                           DestinationName = f.DestinationAirport.Name,
                           DefaultAircraftTail = f.DefaultAircraft != null ? f.DefaultAircraft.TailNumber : null,
                           IsActive = f.IsActive
                       })
                       .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Flight?> GetFlightByAirlineAndFlightNumberAsync(
        string airlineIataCode, string flightNumber, CancellationToken cancellationToken)
    {
        var flightDb = await _context.Flights
                                     .AsNoTracking()
                                     .Where(f => f.Airline.Iatacode == airlineIataCode && f.FlightNumber == flightNumber)
                                     .SingleOrDefaultAsync(cancellationToken);

        return _mapper.Map<Flight>(flightDb);
    }

    public async Task<Flight> GetByAirlineIdAndFlightNumberAsync(
        int airlineId, string flightNumber, CancellationToken cancellationToken)
    {
        var flightDb = await _context.Flights
                                     .AsNoTracking()
                                     .Where(f => f.AirlineId == airlineId && f.FlightNumber == flightNumber)
                                     .SingleOrDefaultAsync(cancellationToken);

        return _mapper.Map<Flight>(flightDb);
    }

    public async Task<bool> HasAnyFlightSchedulesAsync(int flightId, CancellationToken cancellationToken)
    {
        return await _context.FlightSchedules
                             .AsNoTracking()
                             .AnyAsync(fs => fs.FlightId == flightId, cancellationToken);
    }

    public async Task AddAndSaveAsync(Flight flight, CancellationToken cancellationToken)
    {
        var flightDb = _mapper.Map<FlightDb>(flight);
        await _context.Flights.AddAsync(flightDb, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _mapper.Map(flightDb, flight);
    }
}
