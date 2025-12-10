using AirportTool.Domain;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public partial class AirportDbContext : DbContext
{
    public AirportDbContext()
    {
    }

    public AirportDbContext(DbContextOptions<AirportDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AircraftDb> Aircraft { get; set; }

    public virtual DbSet<AirlineDb> Airlines { get; set; }

    public virtual DbSet<AirportDb> Airports { get; set; }

    public virtual DbSet<BookingDb> Bookings { get; set; }

    public virtual DbSet<FlightDb> Flights { get; set; }

    public virtual DbSet<FlightScheduleDb> FlightSchedules { get; set; }

    public virtual DbSet<GateDb> Gates { get; set; }

    public virtual DbSet<TicketDb> Tickets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AircraftDb>(entity =>
        {
            entity.HasKey(e => e.AircraftId).HasName("PK__Aircraft__F75CBC6B63B6EE99");

            entity.HasOne(d => d.OwnedByAirline).WithMany(p => p.Aircraft).HasConstraintName("FK__Aircraft__OwnedB__2F10007B");
        });

        modelBuilder.Entity<AirlineDb>(entity =>
        {
            entity.HasKey(e => e.AirlineId).HasName("PK__Airline__DC4582134EF48B3D");

            entity.Property(e => e.Iatacode).IsFixedLength();
        });

        modelBuilder.Entity<AirportDb>(entity =>
        {
            entity.HasKey(e => e.AirportId).HasName("PK__Airport__E3DBE0EAA277D700");

            entity.Property(e => e.Iatacode).IsFixedLength();
        });

        modelBuilder.Entity<BookingDb>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__73951AED7F59B4F5");

            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Ticket).WithMany(p => p.Bookings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__TicketI__4BAC3F29");
        });

        modelBuilder.Entity<FlightDb>(entity =>
        {
            entity.HasKey(e => e.FlightId).HasName("PK__Flight__8A9E14EE588934E6");

            entity.HasIndex(e => new { e.AirlineId, e.FlightNumber }, "IX_Flight_Active").HasFilter("([IsActive]=(1))");

            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Airline).WithMany(p => p.Flights)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Flight__AirlineI__32E0915F");

            entity.HasOne(d => d.DefaultAircraft).WithMany(p => p.Flights).HasConstraintName("FK__Flight__DefaultA__35BCFE0A");

            entity.HasOne(d => d.DestinationAirport).WithMany(p => p.FlightDestinationAirports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Flight__Destinat__34C8D9D1");

            entity.HasOne(d => d.OriginAirport).WithMany(p => p.FlightOriginAirports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Flight__OriginAi__33D4B598");
        });

        modelBuilder.Entity<FlightScheduleDb>(entity =>
        {
            entity.HasKey(e => e.FlightScheduleId).HasName("PK__FlightSc__AD6AD87CDCDCAB3B");

            entity.HasOne(d => d.AssignedAircraft).WithMany(p => p.FlightSchedules).HasConstraintName("FK__FlightSch__Assig__3D5E1FD2");

            entity.HasOne(d => d.Flight).WithMany(p => p.FlightSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FlightSch__Fligh__3B75D760");

            entity.HasOne(d => d.Gate).WithMany(p => p.FlightSchedules).HasConstraintName("FK__FlightSch__GateI__3C69FB99");
        });

        modelBuilder.Entity<GateDb>(entity =>
        {
            entity.HasKey(e => e.GateId).HasName("PK__Gate__9582C65013E36BE3");

            entity.HasOne(d => d.Airport).WithMany(p => p.Gates)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Gate__AirportId__2B3F6F97");
        });

        modelBuilder.Entity<TicketDb>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__Ticket__712CC607C4741B68");

            entity.Property(e => e.Currency).IsFixedLength();
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TotalPrice).HasComputedColumnSql("([BasePrice]+[Taxes])", true);

            entity.Property(e => e.FareClass)
        .HasMaxLength(2)
        .IsRequired()
        .HasConversion(
            v => FareClassConverter.FareClassToCode(v),   
            v => FareClassConverter.CodeToFareClass(v));

            entity.HasOne(d => d.FlightSchedule).WithMany(p => p.Tickets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ticket__FlightSc__4316F928");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
