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
}
