using AirportTool.Domain;
using AirportTool.Infrastructure;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AirportTool.Tests.Infrastructure.Repositories;

public class GateRepositoryTests : RepositoryTestBase
{
    #region GetDtoByIdAsync

    [Fact]
    public async Task GetDtoByIdAsync_WhenGateDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new GateRepository(ctx, mapper.Object);

        // Act
        var dto = await repo.GetDtoByIdAsync(999, CancellationToken.None);

        // Assert
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetDtoByIdAsync_WhenGateExists_ReturnsDtoWithAirportFields()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var gateDb = CreateGateDb();

        ctx.Gates.Add(gateDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new GateRepository(ctx, mapper.Object);

        // Act
        var dto = await repo.GetDtoByIdAsync(gateDb.GateId, CancellationToken.None);

        // Assert
        dto.Should().NotBeNull();
        dto!.GateId.Should().Be(gateDb.GateId);
        dto.Code.Should().Be(gateDb.Code);
        dto.AirportIataCode.Should().Be(gateDb.Airport.Iatacode);
        dto.AirportName.Should().Be(gateDb.Airport.Name);
    }

    #endregion

    #region GetByAirportIdAndCodeAsync

    [Fact]
    public async Task GetByAirportIdAndCodeAsync_WhenGateDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Gate?>(null))
              .Returns((Gate?)null);

        var repo = new GateRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByAirportIdAndCodeAsync(123, "X1", CancellationToken.None);

        // Assert
        result.Should().BeNull();
        mapper.Verify(m => m.Map<Gate>(null), Times.Once);
    }

    [Fact]
    public async Task GetByAirportIdAndCodeAsync_WhenGateExists_ReturnsMappedGate()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var gateDb = CreateGateDb();

        ctx.Gates.Add(gateDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var expected = _fixture.Build<Gate>()
                               .With(g => g.GateId, gateDb.GateId)
                               .With(g => g.AirportId, gateDb.AirportId)
                               .With(g => g.Code, gateDb.Code)
                               .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Gate>(It.Is<GateDb>(g => g.GateId == gateDb.GateId)))
              .Returns(expected);

        var repo = new GateRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByAirportIdAndCodeAsync(gateDb.AirportId, gateDb.Code, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        mapper.Verify(m => m.Map<Gate>(It.Is<GateDb>(g => g.GateId == gateDb.GateId)), Times.Once);
    }

    #endregion

    #region GetByCodeAndAirportAsync

    [Fact]
    public async Task GetByCodeAndAirportAsync_WhenGateDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Gate?>(null))
              .Returns((Gate?)null);

        var repo = new GateRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByCodeAndAirportAsync("Code", 999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        mapper.Verify(m => m.Map<Gate>(null), Times.Once);
    }

    [Fact]
    public async Task GetByCodeAndAirportAsync_WhenGateExists_ReturnsMappedGate()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var gateDb = CreateGateDb();

        ctx.Gates.Add(gateDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var expected = _fixture.Build<Gate>()
                               .With(g => g.GateId, gateDb.GateId)
                               .With(g => g.AirportId, gateDb.AirportId)
                               .With(g => g.Code, gateDb.Code)
                               .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Gate>(It.Is<GateDb>(g => g.GateId == gateDb.GateId)))
              .Returns(expected);

        var repo = new GateRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByCodeAndAirportAsync(gateDb.Code, gateDb.AirportId, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        mapper.Verify(m => m.Map<Gate>(It.Is<GateDb>(g => g.GateId == gateDb.GateId)), Times.Once);
    }

    #endregion

    #region AddAndSaveAsync

    [Fact]
    public async Task AddAndSaveAsync_WhenGateIsValid_PersistsGateAndUpdatesDomain()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var airportDb = CreateAirportDb();
        ctx.Airports.Add(airportDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var gate = _fixture.Build<Gate>()
                           .Without(g => g.GateId)
                           .With(g => g.AirportId, airportDb.AirportId)
                           .Create();

        var mappedEntity = _fixture.Build<GateDb>()
                                   .Without(g => g.GateId)         
                                   .With(g => g.AirportId, airportDb.AirportId)
                                   .Without(g => g.Airport) 
                                   .Without(g => g.FlightSchedules)
                                   .With(g => g.Code, gate.Code)
                                   .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);

        mapper.Setup(m => m.Map<GateDb>(gate))
              .Returns(mappedEntity);

        mapper.Setup(m => m.Map(mappedEntity, gate))
              .Returns(gate)
              .Callback(() =>
              {
                  gate.GateId = mappedEntity.GateId;
              });

        var repo = new GateRepository(ctx, mapper.Object);

        // Act
        await repo.AddAndSaveAsync(gate, CancellationToken.None);

        // Assert
        var persisted = await ctx.Gates.AsNoTracking()
                                       .SingleAsync(g => g.GateId == gate.GateId);

        persisted.Should().NotBeNull();
        gate.GateId.Should().Be(persisted.GateId);
        gate.AirportId.Should().Be(persisted.AirportId);
        gate.Code.Should().Be(persisted.Code);

        mapper.Verify(m => m.Map<GateDb>(gate), Times.Once);
        mapper.Verify(m => m.Map(mappedEntity, gate), Times.Once);
    }

    #endregion
}
