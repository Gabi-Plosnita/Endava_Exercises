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
    public async Task GetByTailNumberAsync_NoMatch_ReturnsNull()
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
    public async Task GetByTailNumberAsync_MatchExists_ReturnsMappedAircraft()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var airlineDb = _fixture.Build<AirlineDb>()
                                .Without(a => a.Aircraft)
                                .Without(a => a.Flights)
                                .Create();

        var tailNumber = _fixture.Create<string>();
        var aircraftDb = _fixture.Build<AircraftDb>()
                                 .With(a => a.TailNumber, tailNumber)
                                 .With(a => a.OwnedByAirlineId, airlineDb.AirlineId)
                                 .With(a => a.OwnedByAirline, airlineDb)
                                 .Without(a => a.Flights)
                                 .Without(a => a.FlightSchedules)
                                 .Create();

        ctx.Aircraft.Add(aircraftDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var expected = _fixture.Build<Aircraft>()
                               .With(a => a.TailNumber, tailNumber)
                               .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Aircraft>(It.Is<AircraftDb>(a =>a.AircraftId == aircraftDb.AircraftId )))
              .Returns(expected);


        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByTailNumberAsync(tailNumber, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        mapper.Verify(m => m.Map<Aircraft>(It.Is<AircraftDb>(a =>a.AircraftId == aircraftDb.AircraftId)), Times.Once);
    }

    #endregion

    #region GetDtoByIdAsync

    [Fact]
    public async Task GetDtoByIdAsync_NoMatch_ReturnsNull()
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
        mapper.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDtoByIdAsync_AircraftHasAssociatedAirline_ReturnsDtoWithAirlineFields()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var airlineDb = _fixture.Build<AirlineDb>()
                                .Without(a => a.Aircraft)
                                .Without(a => a.Flights)
                                .Create();

        var aircraftDb = _fixture.Build<AircraftDb>()
                                 .With(a => a.OwnedByAirlineId, airlineDb.AirlineId)
                                 .With(a => a.OwnedByAirline, airlineDb)
                                 .Without(a => a.Flights)
                                 .Without(a => a.FlightSchedules)
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
        dto.OwnedByAirlineIataCode.Should().Be(airlineDb.Iatacode);
        dto.OwnedByAirlineName.Should().Be(airlineDb.Name);
    }

    [Fact]
    public async Task GetDtoByIdAsync_AircraftHasNoAssociatedAirline_ReturnsDtoWithNullAirlineFields()
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
    public async Task GetDtoByFilterAsync_NoFilter_ReturnsPagedResultWithTotalCountAndOrderedItems()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        ctx.Aircraft.AddRange(
            _fixture.Build<AircraftDb>().With(a => a.AircraftId, 1).With(a => a.TailNumber, "T1").Without(a => a.OwnedByAirline).Create(),
            _fixture.Build<AircraftDb>().With(a => a.AircraftId, 2).With(a => a.TailNumber, "T2").Without(a => a.OwnedByAirline).Create(),
            _fixture.Build<AircraftDb>().With(a => a.AircraftId, 3).With(a => a.TailNumber, "T3").Without(a => a.OwnedByAirline).Create()
        );

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
        result.Items[0].AircraftId.Should().Be(1);
        result.Items[1].AircraftId.Should().Be(2);

        mapper.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDtoByFilterAsync_FilterByAirlineIataCode_ReturnsOnlyMatchingAircraftAndFilteredTotalCount()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var ro = _fixture.Build<AirlineDb>()
            .With(a => a.AirlineId, 1)
            .With(a => a.Iatacode, "RO")
            .With(a => a.Name, "Tarom")
            .Create();

        var lh = _fixture.Build<AirlineDb>()
            .With(a => a.AirlineId, 2)
            .With(a => a.Iatacode, "LH")
            .With(a => a.Name, "Lufthansa")
            .Create();

        ctx.Airlines.AddRange(ro, lh);

        ctx.Aircraft.AddRange(
            _fixture.Build<AircraftDb>()
                .With(a => a.AircraftId, 1)
                .With(a => a.TailNumber, "RO-1")
                .With(a => a.OwnedByAirline, ro)
                .Create(),
            _fixture.Build<AircraftDb>()
                .With(a => a.AircraftId, 2)
                .With(a => a.TailNumber, "LH-1")
                .With(a => a.OwnedByAirline, lh)
                .Create(),
            _fixture.Build<AircraftDb>()
                .With(a => a.AircraftId, 3)
                .With(a => a.TailNumber, "RO-2")
                .With(a => a.OwnedByAirline, ro)
                .Create()
        );

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var filter = new AircraftFilterDto
        {
            PageIndex = 0,
            PageSize = 10,
            AirlineIataCode = "RO"
        };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetDtoByFilterAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Count.Should().Be(2);
        result.Items.Select(i => i.TailNumber).Should().BeEquivalentTo(new[] { "RO-1", "RO-2" });
        result.Items.All(i => i.OwnedByAirlineIataCode == "RO").Should().BeTrue();

        mapper.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDtoByFilterAsync_PagedSecondPage_ReturnsCorrectSlice()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        ctx.Aircraft.AddRange(
            _fixture.Build<AircraftDb>().With(a => a.AircraftId, 1).With(a => a.TailNumber, "T1").Without(a => a.OwnedByAirline).Create(),
            _fixture.Build<AircraftDb>().With(a => a.AircraftId, 2).With(a => a.TailNumber, "T2").Without(a => a.OwnedByAirline).Create(),
            _fixture.Build<AircraftDb>().With(a => a.AircraftId, 3).With(a => a.TailNumber, "T3").Without(a => a.OwnedByAirline).Create(),
            _fixture.Build<AircraftDb>().With(a => a.AircraftId, 4).With(a => a.TailNumber, "T4").Without(a => a.OwnedByAirline).Create()
        );

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var filter = new AircraftFilterDto
        {
            PageIndex = 1, // second page
            PageSize = 2,
            AirlineIataCode = null
        };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new AircraftRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetDtoByFilterAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(4);
        result.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(2);

        result.Items.Count.Should().Be(2);
        result.Items[0].AircraftId.Should().Be(3);
        result.Items[1].AircraftId.Should().Be(4);

        mapper.VerifyNoOtherCalls();
    }

    #endregion

    #region AddAndSaveAsync

    [Fact]
    public async Task AddAndSaveAsync_ValidAircraft_AddsSavesAndMapsBackIntoDomain()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var aircraft = _fixture.Create<Aircraft>();

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
}
