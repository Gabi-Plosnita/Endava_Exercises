using AirportTool.Domain;

namespace AirportTool.Application;

public interface ITicketRepository : IRepository<Ticket, long>
{
    Task<GetTicketDto?> GetDtoByIdAsync(long ticketId, CancellationToken cancellationToken);

    Task<IReadOnlyList<GetTicketDto>> GetByFlightScheduleIdAsync(int flightScheduleId, CancellationToken cancellationToken);

    Task AddAndSaveAsync(Ticket ticket, CancellationToken cancellationToken);

    Task<bool> HasBookingsAsync(long ticketId, CancellationToken cancellationToken);

    Task<bool> FareClassExistsForScheduleAsync(int flightScheduleId, FareClass fareClass, long? excludeTicketId, CancellationToken ct);
}
