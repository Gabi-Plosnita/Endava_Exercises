using AirportTool.Domain;
using AirportTool.Infrastructure;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AirportTool.Tests.Infrastructure.Repositories;

public class FlightRepositoryTests : RepositoryTestBase
{
    #region GetDtoByIdAsync

    [Fact]
    public async Task GetDtoByIdAsync_WhenFlightDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        var dto = await repo.GetDtoByIdAsync(999, CancellationToken.None);

        // Assert
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetDtoByIdAsync_WhenFlightExistsAndHasDefaultAircraft_ReturnsDtoWithDefaultAircraftTail()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var flightDb = CreateFlightDbWithDefaultAircraft();

        ctx.Flights.Add(flightDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        var dto = await repo.GetDtoByIdAsync(flightDb.FlightId, CancellationToken.None);

        // Assert
        dto.Should().NotBeNull();
        dto!.FlightId.Should().Be(flightDb.FlightId);
        dto.FlightNumber.Should().Be(flightDb.FlightNumber);

        dto.AirlineIata.Should().Be(flightDb.Airline.Iatacode);
        dto.AirlineName.Should().Be(flightDb.Airline.Name);

        dto.OriginIata.Should().Be(flightDb.OriginAirport.Iatacode);
        dto.OriginName.Should().Be(flightDb.OriginAirport.Name);

        dto.DestinationIata.Should().Be(flightDb.DestinationAirport.Iatacode);
        dto.DestinationName.Should().Be(flightDb.DestinationAirport.Name);

        dto.DefaultAircraftTail.Should().Be(flightDb.DefaultAircraft!.TailNumber);
        dto.IsActive.Should().Be(flightDb.IsActive);
    }

    [Fact]
    public async Task GetDtoByIdAsync_WhenFlightExistsAndHasNoDefaultAircraft_ReturnsDtoWithNullDefaultAircraftTail()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var flightDb = CreateFlightDb();

        ctx.Flights.Add(flightDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        var dto = await repo.GetDtoByIdAsync(flightDb.FlightId, CancellationToken.None);

        // Assert
        dto.Should().NotBeNull();
        dto!.FlightId.Should().Be(flightDb.FlightId);
        dto.DefaultAircraftTail.Should().BeNull();
    }

    #endregion

    #region GetFlightByAirlineAndFlightNumberAsync

    [Fact]
    public async Task GetFlightByAirlineAndFlightNumberAsync_WhenFlightDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Flight?>(null))
              .Returns((Flight?)null);

        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetFlightByAirlineAndFlightNumberAsync("ZZ", "ZZ999", CancellationToken.None);

        // Assert
        result.Should().BeNull();
        mapper.Verify(m => m.Map<Flight>(null), Times.Once);
    }

    [Fact]
    public async Task GetFlightByAirlineAndFlightNumberAsync_WhenFlightExists_ReturnsMappedFlight()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var flightDb = CreateFlightDb();
        ctx.Flights.Add(flightDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var expected = _fixture.Build<Flight>()
                               .With(f => f.FlightNumber, flightDb.FlightNumber)
                               .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Flight>(It.Is<FlightDb>(f => f.FlightId == flightDb.FlightId)))
              .Returns(expected);

        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetFlightByAirlineAndFlightNumberAsync(
            flightDb.Airline.Iatacode, flightDb.FlightNumber, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        mapper.Verify(m => m.Map<Flight>(It.Is<FlightDb>(f => f.FlightId == flightDb.FlightId)), Times.Once);
    }

    #endregion

    #region GetByAirlineIdAndFlightNumberAsync

    [Fact]
    public async Task GetByAirlineIdAndFlightNumberAsync_WhenFlightDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Flight?>(null))
              .Returns((Flight?)null);

        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByAirlineIdAndFlightNumberAsync(12345, "NOPE", CancellationToken.None);

        // Assert
        result.Should().BeNull();
        mapper.Verify(m => m.Map<Flight>(null), Times.Once);
    }

    [Fact]
    public async Task GetByAirlineIdAndFlightNumberAsync_WhenFlightExists_ReturnsMappedFlight()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var flightDb = CreateFlightDb();
        ctx.Flights.Add(flightDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var airlineId = flightDb.Airline.AirlineId;

        var expected = _fixture.Build<Flight>()
                               .With(f => f.FlightNumber, flightDb.FlightNumber)
                               .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Flight>(It.Is<FlightDb>(f => f.FlightId == flightDb.FlightId)))
              .Returns(expected);

        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByAirlineIdAndFlightNumberAsync(airlineId, flightDb.FlightNumber, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        mapper.Verify(m => m.Map<Flight>(It.Is<FlightDb>(f => f.FlightId == flightDb.FlightId)), Times.Once);
    }

    #endregion

    #region HasAnyFlightSchedulesAsync

    [Fact]
    public async Task HasAnyFlightSchedulesAsync_WhenNoSchedulesExist_ReturnsFalse()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var flightDb = CreateFlightDb();
        ctx.Flights.Add(flightDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        var result = await repo.HasAnyFlightSchedulesAsync(flightDb.FlightId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAnyFlightSchedulesAsync_WhenSchedulesExist_ReturnsTrue()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var flightScheduleDb = CreateFlightScheduleDb();

        ctx.FlightSchedules.Add(flightScheduleDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        var result = await repo.HasAnyFlightSchedulesAsync(flightScheduleDb.FlightId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region AddAndSaveAsync

    [Fact]
    public async Task AddAndSaveAsync_WhenValidFlight_PersistsFlightAndUpdatesDomain()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var flight = _fixture.Build<Flight>()
                             .Create();

        var mappedEntity = CreateFlightDb();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);

        mapper.Setup(m => m.Map<FlightDb>(flight))
              .Returns(mappedEntity);

        mapper.Setup(m => m.Map(mappedEntity, flight))
              .Returns(flight)
              .Callback(() =>
              {
                  flight.FlightId = mappedEntity.FlightId;
              });

        var repo = new FlightRepository(ctx, mapper.Object);

        // Act
        await repo.AddAndSaveAsync(flight, CancellationToken.None);

        // Assert
        var persisted = await ctx.Flights.AsNoTracking()
                                         .SingleAsync(f => f.FlightId == flight.FlightId);

        persisted.Should().NotBeNull();
        flight.FlightId.Should().Be(persisted.FlightId);

        mapper.Verify(m => m.Map<FlightDb>(flight), Times.Once);
        mapper.Verify(m => m.Map(mappedEntity, flight), Times.Once);
    }

    #endregion
}
