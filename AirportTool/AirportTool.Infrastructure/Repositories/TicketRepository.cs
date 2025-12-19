using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public class TicketRepository : EfRepositoryBase<Ticket, TicketDb, long>, ITicketRepository
{
    public TicketRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }

    public async Task<IReadOnlyList<GetTicketDto>> GetByFlightScheduleIdAsync(
        int flightScheduleId, CancellationToken cancellationToken)
    {
        var getTicketDtos = await _context.Tickets
                                    .AsNoTracking()
                                    .Where(t => t.FlightScheduleId == flightScheduleId)
                                    .Select(t => new GetTicketDto
                                    {
                                        TicketId = t.TicketId,
                                        FlightScheduleId = t.FlightScheduleId,
                                        FareClass = t.FareClass,
                                        BasePrice = t.BasePrice,
                                        Taxes = t.Taxes,
                                        TotalPrice = t.TotalPrice,
                                        Currency = t.Currency,
                                        IsRefundable = t.IsRefundable,
                                        SeatInventory = t.SeatInventory
                                    })
                                    .ToListAsync(cancellationToken);

        return getTicketDtos;
    }

    public Task<bool> HasBookingsAsync(long ticketId, CancellationToken cancellationToken)
    {
        return _context.Bookings
                       .AsNoTracking()
                       .AnyAsync(b => b.TicketId == ticketId, cancellationToken);
    }
}
