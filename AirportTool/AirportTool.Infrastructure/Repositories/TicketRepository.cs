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

    public Task<GetTicketDto?> GetDtoByIdAsync(long ticketId, CancellationToken cancellationToken)
    {
        return _context.Tickets
                       .AsNoTracking()
                       .Where(t => t.TicketId == ticketId)
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
                       .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GetTicketDto>> GetByFlightScheduleIdAsync(int flightScheduleId, CancellationToken cancellationToken)
    {
        return await _context.Tickets
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
    }

    public async Task AddAndSaveAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        var ticketDb = _mapper.Map<TicketDb>(ticket);
        await _context.Tickets.AddAsync(ticketDb, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _mapper.Map(ticketDb, ticket);
    }

    public Task<bool> HasBookingsAsync(long ticketId, CancellationToken cancellationToken)
    {
        return _context.Bookings
                       .AsNoTracking()
                       .AnyAsync(b => b.TicketId == ticketId, cancellationToken);
    }

    public Task<bool> FareClassExistsForScheduleAsync(
        int flightScheduleId, FareClass fareClass, long? excludeTicketId, CancellationToken cancellationToken)
    {
        var querry = _context.Tickets.AsNoTracking()
                                     .Where(t => t.FlightScheduleId == flightScheduleId && t.FareClass == fareClass);

        if (excludeTicketId.HasValue)
        {
            querry = querry.Where(t => t.TicketId != excludeTicketId.Value);
        }

        return querry.AnyAsync(cancellationToken);
    }
}
