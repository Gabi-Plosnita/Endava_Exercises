using AirportTool.Domain;
using AirportTool.Infrastructure;
using AutoMapper;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AirportTool.Tests.Infrastructure.Repositories;

public class EfRepositoryBaseTests
{
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
        ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
        ctx.Database.EnsureCreated();

        return (ctx, conn);
    }

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var repo = new TestGateRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByIdAsync(999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_DetachesEntityAndReturnsMappedDomain()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var gateDb = new GateDb { GateId = 1, AirportId = 123, Code = "A1" };
        ctx.Gates.Add(gateDb);
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();

        var expectedDomain = new Gate { GateId = 1, AirportId = 123, Code = "A1" };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Gate>(It.IsAny<GateDb>()))
              .Returns(expectedDomain);

        var repo = new TestGateRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByIdAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedDomain);
        ctx.ChangeTracker.Entries<GateDb>().Should().BeEmpty();

        mapper.Verify(m => m.Map<Gate>(It.Is<GateDb>(g => g.GateId == 1)), Times.Once);
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenEntitiesExist_ReturnsMappedList()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        ctx.Gates.AddRange(
            new GateDb { GateId = 1, AirportId = 10, Code = "A1" },
            new GateDb { GateId = 2, AirportId = 10, Code = "A2" }
        );
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var expected = (IReadOnlyList<Gate>)new List<Gate>
        {
            new Gate { GateId = 1, AirportId = 10, Code = "A1" },
            new Gate { GateId = 2, AirportId = 10, Code = "A2" }
        };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<IReadOnlyList<Gate>>(It.IsAny<List<GateDb>>()))
              .Returns(expected);

        var repo = new TestGateRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);

        mapper.Verify(m => m.Map<IReadOnlyList<Gate>>(It.Is<List<GateDb>>(list => list.Count == 2)), Times.Once);
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_WhenDomainIsValid_AddsEntityWithAddedState()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var domain = new Gate { AirportId = 10, Code = "A1" };
        var entity = new GateDb { AirportId = 10, Code = "A1" };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<GateDb>(domain)).Returns(entity);

        var repo = new TestGateRepository(ctx, mapper.Object);

        // Act
        await repo.AddAsync(domain, CancellationToken.None);

        // Assert
        ctx.Entry(entity).State.Should().Be(EntityState.Added);

        mapper.Verify(m => m.Map<GateDb>(domain), Times.Once);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenDomainIsValid_MarksEntityAsModified()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var domain = new Gate { GateId = 1, AirportId = 10, Code = "A1" };
        var entity = new GateDb { GateId = 1, AirportId = 10, Code = "A1" };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<GateDb>(domain)).Returns(entity);

        var repo = new TestGateRepository(ctx, mapper.Object);

        // Act
        await repo.UpdateAsync(domain, CancellationToken.None);

        // Assert
        ctx.Entry(entity).State.Should().Be(EntityState.Modified);

        mapper.Verify(m => m.Map<GateDb>(domain), Times.Once);
    }

    #endregion

    #region RemoveAsync

    [Fact]
    public async Task RemoveAsync_WhenDomainIsValid_MarksEntityAsDeleted()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var domain = new Gate { GateId = 1, AirportId = 10, Code = "A1" };
        var entity = new GateDb { GateId = 1, AirportId = 10, Code = "A1" };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<GateDb>(domain)).Returns(entity);

        var repo = new TestGateRepository(ctx, mapper.Object);

        // Act
        await repo.RemoveAsync(domain, CancellationToken.None);

        // Assert
        ctx.Entry(entity).State.Should().Be(EntityState.Deleted);

        mapper.Verify(m => m.Map<GateDb>(domain), Times.Once);
    }

    #endregion
}

internal sealed class TestGateRepository : EfRepositoryBase<Gate, GateDb, int>
{
    public TestGateRepository(AirportDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }
}
