using AirportTool.Domain;

namespace AirportTool.Application;

public interface ITicketRepository : IRepository<Ticket, long>
{
    Task<IReadOnlyList<Ticket>> GetTicketsByFlightScheduleIdAsync(
        int flightScheduleId, CancellationToken cancellationToken);
}
