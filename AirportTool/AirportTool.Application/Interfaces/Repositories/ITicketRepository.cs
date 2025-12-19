using AirportTool.Domain;

namespace AirportTool.Application;

public interface ITicketRepository : IRepository<Ticket, long>
{
    Task<IReadOnlyList<GetTicketDto>> GetByFlightScheduleIdAsync(int flightScheduleId, CancellationToken cancellationToken);

    Task<bool> HasBookingsAsync(long ticketId, CancellationToken cancellationToken);
}
