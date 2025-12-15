using AirportTool.Application;

namespace AirportTool.Infrastructure;

public sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly AirportDbContext _context;
    public IFlightRepository Flights { get; }
    public IFlightScheduleRepository FlightSchedules { get; }
    public ITicketRepository Tickets { get; }
    public IBookingRepository Bookings { get; }

    public UnitOfWork(
        AirportDbContext context,
        IFlightRepository flightRepository,
        IFlightScheduleRepository flightScheduleRepository,
        ITicketRepository ticketRepository,
        IBookingRepository bookingRepository)
    {
        _context = context;
        Flights = flightRepository;
        FlightSchedules = flightScheduleRepository;
        Tickets = ticketRepository;
        Bookings = bookingRepository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _context.DisposeAsync();
}

