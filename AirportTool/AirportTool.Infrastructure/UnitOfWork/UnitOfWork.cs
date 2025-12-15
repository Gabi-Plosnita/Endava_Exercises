using AirportTool.Application;

namespace AirportTool.Infrastructure;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly AirportDbContext _context;
    public IFlightRepository Flights { get; }
    public IFlightScheduleRepository FlightSchedules { get; }
    public ITicketRepository Tickets { get; }
    public IBookingRepository Bookings { get; }
    public IAirlineRepository Airline { get; }

    public UnitOfWork(
        AirportDbContext context,
        IFlightRepository flightRepository,
        IFlightScheduleRepository flightScheduleRepository,
        ITicketRepository ticketRepository,
        IBookingRepository bookingRepository,
        IAirlineRepository airlineRepository)
    {
        _context = context;
        Flights = flightRepository;
        FlightSchedules = flightScheduleRepository;
        Tickets = ticketRepository;
        Bookings = bookingRepository;
        Airline = airlineRepository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _context.DisposeAsync();
}

