namespace AirportTool.Application;

public interface IUnitOfWork
{
    public IFlightRepository Flights { get; }
    public IFlightScheduleRepository FlightSchedules { get; }
    public ITicketRepository Tickets { get; }
    public IBookingRepository Bookings { get; }
    public IAirlineRepository Airlines { get; }
    public IAirportRepository Airports { get; }
    public IAircraftRepository Aircrafts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
