using AirportTool.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Tests.Infrastructure.Repositories;

internal sealed class TestAirportDbContext : AirportDbContext
{
    public TestAirportDbContext(DbContextOptions<AirportDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TicketDb>()
            .Property(t => t.RowVersion)
            .IsRequired()
            .ValueGeneratedNever();

        modelBuilder.Entity<BookingDb>()
            .Property(b => b.RowVersion)
            .IsRequired()
            .ValueGeneratedNever();
    }
}
