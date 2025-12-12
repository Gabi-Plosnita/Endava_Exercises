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

    public async Task<Booking?> GetByConfirmationCodeAsync(
        string confirmationCode, CancellationToken cancellationToken = default)
    {
        var bookingDb = await _context.Bookings
                                      .AsNoTracking()
                                      .SingleOrDefaultAsync(b => b.ConfirmationCode == confirmationCode, cancellationToken);
       
        return _mapper.Map<Booking>(bookingDb);
    }
}
