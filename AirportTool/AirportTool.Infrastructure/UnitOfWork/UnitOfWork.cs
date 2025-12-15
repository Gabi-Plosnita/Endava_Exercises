using AirportTool.Application;

namespace AirportTool.Infrastructure;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly AirportDbContext _context;
    public IFlightRepository Flights { get; }
    public IFlightScheduleRepository FlightSchedules { get; }
    public ITicketRepository Tickets { get; }
    public IBookingRepository Bookings { get; }
    public IAirlineRepository Airlines { get; }
    public IAirportRepository Airports { get; }
    public IAircraftRepository Aircrafts { get; }


    public UnitOfWork(
        AirportDbContext context,
        IFlightRepository flightRepository,
        IFlightScheduleRepository flightScheduleRepository,
        ITicketRepository ticketRepository,
        IBookingRepository bookingRepository,
        IAirlineRepository airlineRepository,
        IAirportRepository airportRepository,
        IAircraftRepository aircraftRepository)
    {
        _context = context;
        Flights = flightRepository;
        FlightSchedules = flightScheduleRepository;
        Tickets = ticketRepository;
        Bookings = bookingRepository;
        Airlines = airlineRepository;
        Airports = airportRepository;
        Aircrafts = aircraftRepository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _context.DisposeAsync();
}

