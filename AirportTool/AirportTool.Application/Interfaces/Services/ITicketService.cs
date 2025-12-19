namespace AirportTool.Application;

public interface ITicketService
{
    Task<Result<IReadOnlyList<GetTicketDto>>> GetByFlightScheduleIdAsync(int flightScheduleId, CancellationToken cancellationToken);

    Task<Result<GetTicketDto?>> CreateAsync(CreateTicketDto dto, CancellationToken cancellationToken);

    Task<Result> UpdateAsync(long ticketId, UpdateTicketDto dto, CancellationToken cancellationToken);

    Task<Result> DeleteByIdAsync(long ticketId, CancellationToken cancellationToken);
}
