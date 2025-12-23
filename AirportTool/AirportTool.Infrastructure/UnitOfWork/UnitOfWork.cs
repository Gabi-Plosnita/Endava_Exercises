using AirportTool.Application;
using Microsoft.Data.SqlClient;
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
            throw new DatabaseConcurrencyException("The entity was modified by another operation.", ex);
        }
        catch (DbUpdateException ex)
        {
            var baseEx = ex.GetBaseException();

            if (baseEx is SqlException sqlEx)
            {
                switch (sqlEx.Number)
                {
                    case 2601:
                    case 2627:
                        throw new DatabaseUniqueConstraintException("Duplicate key.", ex);

                    case 547:
                        throw new DatabaseConstraintException("FK/CHECK constraint violation.", ex);

                    case 515:
                        throw new DatabaseNotNullException("NOT NULL constraint violation.", ex);

                    case 2628:
                    case 8152: 
                        throw new DatabaseDataTooLongException("String/bytes truncated.", ex);

                    case -2:
                        throw new DatabaseTimeoutException("Database timeout.", ex);

                    default:
                        throw new DatabaseWriteException("Database update failed.", ex);
                }
            }

            throw new DatabaseWriteException("Database update failed.", ex);
        }
    }

}

