using AirportTool.Domain;

namespace AirportTool.Application;

public interface ITicketRepository : IRepository<Ticket, long>
{
    Task<IReadOnlyCollection<Ticket>> GetTicketsByFlightScheduleIdAsync(
        int flightScheduleId,
        CancellationToken cancellationToken = default);
}
