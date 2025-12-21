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

    public Task<GetBookingDto?> GetDtoByConfirmationCodeAsync(
        string confirmationCode, CancellationToken cancellationToken)
    {
        return _context.Bookings
                       .AsNoTracking()
                       .Where(b => b.ConfirmationCode == confirmationCode)
                       .Select(b => new GetBookingDto
                       {
                           BookingId = b.BookingId,
                           TicketId = b.TicketId,
                           PassengerFullName = b.PassengerFullName,
                           PassengerEmail = b.PassengerEmail,
                           ConfirmationCode = b.ConfirmationCode,
                           Quantity = b.Quantity,
                           TotalAmount = b.Quantity * (b.Ticket.BasePrice + b.Ticket.Taxes),
                           Status = b.Status
                       })
                       .SingleOrDefaultAsync(cancellationToken);
    }
}
