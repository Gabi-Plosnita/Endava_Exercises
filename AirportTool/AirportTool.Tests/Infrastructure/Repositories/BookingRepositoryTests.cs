using AirportTool.Domain;
using AirportTool.Infrastructure;
using AutoFixture;
using AutoMapper;
using AwesomeAssertions;
using Moq;

namespace AirportTool.Tests.Infrastructure.Repositories;

public class BookingRepositoryTests : RepositoryTestBase
{
    #region GetByConfirmationCodeAsync

    [Fact]
    public async Task GetByConfirmationCodeAsync_WhenBookingDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Booking?>(null))
              .Returns((Booking?)null);

        var repo = new BookingRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByConfirmationCodeAsync("ZZZZZZZZ", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByConfirmationCodeAsync_WhenBookingExists_ReturnsMappedBooking()
    {
        // Arrange
        var (ctx, conn) = CreateSqliteInMemoryContext();
        await using var _ = ctx;
        await using var __ = conn;

        var bookingDb = CreateBookingDb();

        ctx.Bookings.Add(bookingDb);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var expected = _fixture.Build<Booking>()
                               .With(b => b.ConfirmationCode, bookingDb.ConfirmationCode)
                               .With(b => b.PassengerEmail, bookingDb.PassengerEmail)
                               .With(b => b.PassengerFullName, bookingDb.PassengerFullName)
                               .With(b => b.Quantity, bookingDb.Quantity)
                               .With(b => b.Status, bookingDb.Status)
                               .With(b => b.CreatedUtc, bookingDb.CreatedUtc)
                               .With(b => b.TicketId, bookingDb.TicketId)
                               .Create();

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper.Setup(m => m.Map<Booking>(It.Is<BookingDb>(b => b.BookingId == bookingDb.BookingId)))
              .Returns(expected);

        var repo = new BookingRepository(ctx, mapper.Object);

        // Act
        var result = await repo.GetByConfirmationCodeAsync(bookingDb.ConfirmationCode, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        mapper.Verify(m => m.Map<Booking>(It.Is<BookingDb>(b => b.BookingId == bookingDb.BookingId)), Times.Once);
    }

    #endregion
}
