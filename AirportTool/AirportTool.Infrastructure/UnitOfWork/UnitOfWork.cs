using AirportTool.Application;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AirportDbContext _context;
    public IFlightRepository Flights { get; }
    public IFlightScheduleRepository FlightSchedules { get; }
    public ITicketRepository Tickets { get; }
    public IBookingRepository Bookings { get; }
    public IAirlineRepository Airlines { get; }
    public IAirportRepository Airports { get; }
    public IAircraftRepository Aircrafts { get; }
    public IGateRepository Gates { get; }

    public UnitOfWork(
        AirportDbContext context,
        IFlightRepository flightRepository,
        IFlightScheduleRepository flightScheduleRepository,
        ITicketRepository ticketRepository,
        IBookingRepository bookingRepository,
        IAirlineRepository airlineRepository,
        IAirportRepository airportRepository,
        IAircraftRepository aircraftRepository,
        IGateRepository gates)
    {
        _context = context;
        Flights = flightRepository;
        FlightSchedules = flightScheduleRepository;
        Tickets = ticketRepository;
        Bookings = bookingRepository;
        Airlines = airlineRepository;
        Airports = airportRepository;
        Aircrafts = aircraftRepository;
        Gates = gates;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The entity was modified by another operation.", ex);
        }
    }
}

