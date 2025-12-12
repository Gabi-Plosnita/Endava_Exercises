using AirportTool.Domain;

namespace AirportTool.Application;

public interface IBookingRepository : IRepository<Booking, long>
{
    Task<Booking?> GetByConfirmationCodeAsync(string confirmationCode, CancellationToken cancellationToken = default);
}
