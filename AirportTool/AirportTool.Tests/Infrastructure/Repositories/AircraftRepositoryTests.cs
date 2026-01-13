using AirportTool.Application;
using AirportTool.Domain;
using AirportTool.Infrastructure;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AirportTool.Tests.Infrastructure.Repositories;

public class AircraftRepositoryTests
{
    private readonly Fixture _fixture;

    public AircraftRepositoryTests()
    {
        _fixture = new Fixture();

        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));

        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    private static (AirportDbContext ctx, SqliteConnection conn) CreateSqliteInMemoryContext()
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

    #region GetByTailNumberAsync

    [Fact]
    public async Task GetByTailNumberAsync_WhenAircraftDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Aircraft?>(null))
              .Returns((Aircraft?)null);

        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByTailNumberAsync("ABC", CancellationToken.None);

        // Assert
        result.Should().BeNull();

        mapper.Verify(m => m.Map<Aircraft>(null), Times.Once);
    }

    [Fact]
    public async Task GetByTailNumberAsync_WhenAircraftExists_ReturnsMappedAircraft()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var airlineDb = CreateAirlineDb();
        var aircraftDb = CreateAircraftDbWithAirline(airlineDb);

        ctx.Aircraft.Add(aircraftDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var expected = _fixture.Build<Aircraft>()
                               .With(a => a.TailNumber, aircraftDb.TailNumber)
                               .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Aircraft>(It.Is<AircraftDb>(a =>a.AircraftId == aircraftDb.AircraftId)))
              .Returns(expected);


        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByTailNumberAsync(aircraftDb.TailNumber, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        mapper.Verify(m => m.Map<Aircraft>(It.Is<AircraftDb>(a =>a.AircraftId == aircraftDb.AircraftId)), Times.Once);
    }

    #endregion

    #region GetDtoByIdAsync

    [Fact]
    public async Task GetDtoByIdAsync__WhenAircraftDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var dto = await repo.GetDtoByIdAsync(999, CancellationToken.None);

        // Assert
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetDtoByIdAsync_WhenAircraftHasAssociatedAirline_ReturnsDtoWithAirlineFields()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var airlineDb = CreateAirlineDb();
        var aircraftDb = CreateAircraftDbWithAirline(airlineDb);

        ctx.Aircraft.Add(aircraftDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var dto = await repo.GetDtoByIdAsync(aircraftDb.AircraftId, CancellationToken.None);

        // Assert
        dto.Should().NotBeNull();
        dto.AircraftId.Should().Be(aircraftDb.AircraftId);
        dto.TailNumber.Should().Be(aircraftDb.TailNumber);
        dto.Model.Should().Be(aircraftDb.Model);
        dto.SeatCapacity.Should().Be(aircraftDb.SeatCapacity);
        dto.OwnedByAirlineIataCode.Should().Be(airlineDb.Iatacode);
        dto.OwnedByAirlineName.Should().Be(airlineDb.Name);
    }

    [Fact]
    public async Task GetDtoByIdAsync_WhenAircraftHasNoAssociatedAirline_ReturnsDtoWithNullAirlineFields()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var aircraftDb = _fixture.Build<AircraftDb>()
                               .Without(a => a.Flights)
                               .Without(a => a.FlightSchedules)
                               .Without(a => a.OwnedByAirline)
                               .Without(a => a.OwnedByAirlineId)
                               .Create();

        ctx.Aircraft.Add(aircraftDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var dto = await repo.GetDtoByIdAsync(aircraftDb.AircraftId, CancellationToken.None);

        // Assert
        dto.Should().NotBeNull();
        dto.AircraftId.Should().Be(aircraftDb.AircraftId);
        dto.TailNumber.Should().Be(aircraftDb.TailNumber);
        dto.Model.Should().Be(aircraftDb.Model);
        dto.SeatCapacity.Should().Be(aircraftDb.SeatCapacity);
        dto.OwnedByAirlineIataCode.Should().BeNull();
        dto.OwnedByAirlineName.Should().BeNull();
    }

    #endregion

    #region GetDtoByFilterAsync

    [Fact]
    public async Task GetDtoByFilterAsync_WhenNoFilterApplied_ReturnsFirstPageOrderedById()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var aircraftDb1 = CreateAircraftDbWithoutAirline();
        var aircraftDb2 = CreateAircraftDbWithoutAirline();
        var aircraftDb3 = CreateAircraftDbWithoutAirline();

        ctx.Aircraft.AddRange(aircraftDb1, aircraftDb2, aircraftDb3);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var filter = new AircraftFilterDto
        {
            PageIndex = 0,
            PageSize = 2,
            AirlineIataCode = null
        };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetDtoByFilterAsync(filter, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.PageIndex.Should().Be(0);
        result.PageSize.Should().Be(2);

        result.Items.Should().NotBeNull();
        result.Items.Count.Should().Be(2);
        result.Items[0].AircraftId.Should().Be(aircraftDb1.AircraftId);
        result.Items[1].AircraftId.Should().Be(aircraftDb2.AircraftId);
    }

    [Fact]
    public async Task GetDtoByFilterAsync_WhenFilteredByAirline_ReturnsOnlyAircraftForThatAirline()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var airline1 = CreateAirlineDb();
        var airline2 = CreateAirlineDb();

        var aircraftDb1 = CreateAircraftDbWithAirline(airline1);
        var aircraftDb2 = CreateAircraftDbWithAirline(airline2);
        var aircraftDb3 = CreateAircraftDbWithAirline(airline1);

        ctx.Aircraft.AddRange(aircraftDb1, aircraftDb2, aircraftDb3);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var filter = new AircraftFilterDto
        {
            PageIndex = 0,
            PageSize = 10,
            AirlineIataCode = airline1.Iatacode
        };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetDtoByFilterAsync(filter, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.PageIndex.Should().Be(0);
        result.PageSize.Should().Be(10);

        result.Items.Should().NotBeNull();
        result.Items.Count.Should().Be(2);
        result.Items[0].AircraftId.Should().Be(aircraftDb1.AircraftId);
        result.Items[1].AircraftId.Should().Be(aircraftDb3.AircraftId);
    }

    [Fact]
    public async Task GetDtoByFilterAsync_WhenRequestingSecondPage_ReturnsSecondPageItems()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var aircraftDb1 = CreateAircraftDbWithoutAirline();
        var aircraftDb2 = CreateAircraftDbWithoutAirline();
        var aircraftDb3 = CreateAircraftDbWithoutAirline();
        var aircraftDb4 = CreateAircraftDbWithoutAirline();

        ctx.Aircraft.AddRange(aircraftDb1, aircraftDb2, aircraftDb3, aircraftDb4);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var filter = new AircraftFilterDto
        {
            PageIndex = 1, 
            PageSize = 2,
            AirlineIataCode = null
        };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetDtoByFilterAsync(filter, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(4);
        result.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(2);

        result.Items.Should().NotBeNull();
        result.Items.Count.Should().Be(2);
        result.Items[0].AircraftId.Should().Be(aircraftDb3.AircraftId);
        result.Items[1].AircraftId.Should().Be(aircraftDb4.AircraftId);
    }

    #endregion

    #region AddAndSaveAsync

    [Fact]
    public async Task AddAndSaveAsync_WhenValidAircraft_PersistsAircraftAndUpdatesDomain()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var aircraft = _fixture.Build<Aircraft>()
                               .Without(a => a.OwnedByAirlineId)
                               .Create();

        var mappedEntity = _fixture.Build<AircraftDb>()
                                   .With(a => a.TailNumber, aircraft.TailNumber)
                                   .With(a => a.Model, aircraft.Model)
                                   .With(a => a.SeatCapacity, aircraft.SeatCapacity)
                                   .Without(a => a.AircraftId)
                                   .Without(a => a.OwnedByAirlineId)
                                   .Without(a => a.OwnedByAirline)
                                   .Without(a => a.Flights)
                                   .Without(a => a.FlightSchedules)
                                   .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);

        mapper.Setup(m => m.Map<AircraftDb>(aircraft))
              .Returns(mappedEntity);

        mapper.Setup(m => m.Map(mappedEntity, aircraft))
              .Returns(aircraft)
              .Callback(() =>
              {
                  aircraft.AircraftId = mappedEntity.AircraftId;
              });

        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        await repo.AddAndSaveAsync(aircraft, CancellationToken.None);

        // Assert
        var persistedEntity = await ctx.Aircraft.AsNoTracking().SingleAsync(a => a.AircraftId == aircraft.AircraftId);

        persistedEntity.Should().NotBeNull();
        aircraft.AircraftId.Should().Be(persistedEntity.AircraftId);
        aircraft.TailNumber.Should().Be(persistedEntity.TailNumber);
        aircraft.Model.Should().Be(persistedEntity.Model);
        aircraft.SeatCapacity.Should().Be(persistedEntity.SeatCapacity);
        aircraft.OwnedByAirlineId.Should().Be(persistedEntity.OwnedByAirlineId);

        mapper.Verify(m => m.Map<AircraftDb>(aircraft), Times.Once);
        mapper.Verify(m => m.Map(mappedEntity, aircraft), Times.Once);
    }

    #endregion

    #region Helper Methods

    private AirlineDb CreateAirlineDb()
    {
        return _fixture.Build<AirlineDb>()
                       .Without(a => a.AirlineId)
                       .Without(a => a.Aircraft)
                       .Without(a => a.Flights)
                       .Create();
    }

    private AircraftDb CreateAircraftDbWithoutAirline()
    {
        return _fixture.Build<AircraftDb>()
                       .Without(a => a.AircraftId)
                       .Without(a => a.Flights)
                       .Without(a => a.FlightSchedules)
                       .Without(a => a.OwnedByAirline)
                       .Without(a => a.OwnedByAirlineId)
                       .Create();
    }

    private AircraftDb CreateAircraftDbWithAirline(AirlineDb airline)
    {
        return _fixture.Build<AircraftDb>()
                       .Without(a => a.AircraftId)
                       .Without(a => a.Flights)
                       .Without(a => a.FlightSchedules)
                       .With(a => a.OwnedByAirline, airline)
                       .Create();
    }

    #endregion
}
