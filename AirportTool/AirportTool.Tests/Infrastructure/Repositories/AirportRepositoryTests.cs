using AirportTool.Domain;
using AirportTool.Infrastructure;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Moq;

namespace AirportTool.Tests.Infrastructure.Repositories;

public class AirportRepositoryTests : RepositoryTestBase
{
    #region GetByIataCodeAsync 

    [Fact]
    public async Task GetByIataCodeAsync_WhenIataCodeDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Airport?>(null))
              .Returns((Airport?)null);

        var repo = new AirportRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByIataCodeAsync("AB", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIataCodeAsync_WhenIataCodeExists_ReturnsMappedAirport()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var airportDb = CreateAirportDb();

        ctx.Airports.Add(airportDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var expected = _fixture.Build<Airport>()
                               .With(a => a.AirportId, airportDb.AirportId)
                               .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Airport>(It.Is<AirportDb>(a => a.AirportId == airportDb.AirportId)))
              .Returns(expected);

        var repo = new AirportRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByIataCodeAsync(airportDb.Iatacode, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        mapper.Verify(m => m.Map<Airport>(It.Is<AirportDb>(a => a.AirportId == airportDb.AirportId)), Times.Once);
    }

    #endregion
}
