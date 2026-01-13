using AirportTool.Domain;
using AirportTool.Infrastructure;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Moq;

namespace AirportTool.Tests.Infrastructure.Repositories;

public class AirlineRepositoryTests : RepositoryTestBase
{
    #region GetByIataCodeAsync

    [Fact]
    public async Task GetByIataCodeAsync_WhenAirlineDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Airline?>(null))
              .Returns((Airline?)null);

        var repo = new AirlineRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByIataCodeAsync("ZZ", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIataCodeAsync_WhenAirlineExists_ReturnsMappedAirline()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var airlineDb = CreateAirlineDb();

        ctx.Airlines.Add(airlineDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var expected = _fixture.Build<Airline>()
                               .With(a => a.Iatacode, airlineDb.Iatacode)
                               .With(a => a.Name, airlineDb.Name)
                               .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Airline>(It.Is<AirlineDb>(a => a.AirlineId == airlineDb.AirlineId)))
              .Returns(expected);

        var repo = new AirlineRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByIataCodeAsync(airlineDb.Iatacode, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        mapper.Verify(m => m.Map<Airline>(It.Is<AirlineDb>(a => a.AirlineId == airlineDb.AirlineId)), Times.Once);
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

    #endregion
}
