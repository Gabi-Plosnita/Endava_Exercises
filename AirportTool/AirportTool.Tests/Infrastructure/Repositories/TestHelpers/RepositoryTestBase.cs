using AirportTool.Infrastructure;
using AutoFixture;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Tests.Infrastructure.Repositories;

public abstract class RepositoryTestBase
{
    protected Fixture _fixture { get; set; }

    protected RepositoryTestBase()
    {
        _fixture = new Fixture();

        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));

        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    protected static (AirportDbContext ctx, SqliteConnection conn) CreateSqliteInMemoryContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder<AirportDbContext>()
            .UseSqlite(conn)
            .EnableSensitiveDataLogging()
            .Options;

        var ctx = new AirportDbContext(options);

        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();

        return (ctx, conn);
    }

    #region Entities Creation Helpers

    protected AirlineDb CreateAirlineDb()
    {
        return _fixture.Build<AirlineDb>()
                       .Without(a => a.AirlineId)
                       .Without(a => a.Aircraft)
                       .Without(a => a.Flights)
                       .Create();
    }

    protected AircraftDb CreateAircraftDbWithoutAirline()
    {
        return _fixture.Build<AircraftDb>()
                       .Without(a => a.AircraftId)
                       .Without(a => a.Flights)
                       .Without(a => a.FlightSchedules)
                       .Without(a => a.OwnedByAirline)
                       .Without(a => a.OwnedByAirlineId)
                       .Create();
    }

    protected AircraftDb CreateAircraftDbWithAirline()
    {
        var airlineDb = CreateAirlineDb();
        return _fixture.Build<AircraftDb>()
                       .Without(a => a.AircraftId)
                       .Without(a => a.Flights)
                       .Without(a => a.FlightSchedules)
                       .With(a => a.OwnedByAirline, airlineDb)
                       .Create();
    }

    protected AircraftDb CreateAircraftDbWithAirline(AirlineDb airlineDb)
    {
        return _fixture.Build<AircraftDb>()
                       .Without(a => a.AircraftId)
                       .Without(a => a.Flights)
                       .Without(a => a.FlightSchedules)
                       .With(a => a.OwnedByAirline, airlineDb)
                       .Create();
    }

    protected AirportDb CreateAirportDb()
    {
        return _fixture.Build<AirportDb>()
                       .Without(a => a.AirportId)
                       .Without(a => a.FlightDestinationAirports)
                       .Without(a => a.FlightOriginAirports)
                       .Without(a => a.Gates)
                       .Create();
    }

    protected FlightDb CreateFlightDb()
    {
        var airlineDb = CreateAirlineDb();
        var originAirportDb = CreateAirportDb();
        var destinationAirportDb = CreateAirportDb();
        return _fixture.Build<FlightDb>()
                       .With(f => f.Airline, airlineDb)
                       .Without(f => f.AirlineId)
                       .With(f => f.OriginAirport, originAirportDb)
                       .Without(f => f.OriginAirportId)
                       .With(f => f.DestinationAirport, destinationAirportDb)
                       .Without(f => f.DestinationAirportId)
                       .Without(f => f.FlightId)
                       .Without(f => f.FlightSchedules)
                       .Without(f => f.DefaultAircraft)
                       .Without(f => f.DefaultAircraftId)
                       .Create();
    }

    protected FlightScheduleDb CreateFlightScheduleDb()
    {
        var flightDb = CreateFlightDb();
        return _fixture.Build<FlightScheduleDb>()
                       .With(fs => fs.Flight, flightDb)
                       .Without(fs => fs.FlightId)
                       .Without(fs => fs.FlightScheduleId)
                       .Without(fs => fs.Tickets)
                       .Without(fs => fs.AssignedAircraft)
                       .Without(fs => fs.AssignedAircraftId)
                       .Create();
    }

    protected TicketDb CreateTicketDb()
    {
        var flightScheduleDb = CreateFlightScheduleDb();
        return _fixture.Build<TicketDb>()
                       .With(t => t.RowVersion, new byte[] { 1 })
                       .With(t => t.FlightSchedule, flightScheduleDb)
                       .Without(t => t.FlightScheduleId)
                       .Without(t => t.TicketId)
                       .Without(t => t.Bookings)
                       .Create();
    }

    protected BookingDb CreateBookingDb()
    {
        var ticketDb = CreateTicketDb();
        return _fixture.Build<BookingDb>()
                       .With(b => b.Ticket, ticketDb)
                       .Without(b => b.TicketId)
                       .With(b => b.RowVersion, new byte[] { 1 })
                       .Without(b => b.BookingId)
                       .Create();
    }

    #endregion
}
