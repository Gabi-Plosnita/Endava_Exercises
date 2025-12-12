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

    public async Task<IReadOnlyCollection<Ticket>> GetTicketsByFlightScheduleIdAsync(
        int flightScheduleId,
        CancellationToken cancellationToken = default)
    {
        var ticketDbs = await _context.Tickets
                                    .AsNoTracking()
                                    .Where(t => t.FlightScheduleId == flightScheduleId)
                                    .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyCollection<Ticket>>(ticketDbs);
    }
}
