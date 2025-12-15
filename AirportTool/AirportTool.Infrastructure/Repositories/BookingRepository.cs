using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public class BookingRepository : EfRepositoryBase<Booking, BookingDb, long>, IBookingRepository
{
    public BookingRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }

    public async Task<Flight?> GetFlightByAirlineAndFlightNumberAsync(
        string airlineIataCode, string flightNumber, CancellationToken cancellationToken = default)
    {
        var flightDb = await _context.Flights
                                     .AsNoTracking()
                                     .Where(f => f.FlightNumber == flightNumber && f.Airline.Iatacode == airlineIataCode)
                                     .FirstOrDefaultAsync(cancellationToken);

        return _mapper.Map<Flight>(flightDb);
    }
}
