using AirportTool.Domain;

namespace AirportTool.Application;

public interface IBookingRepository : IRepository<Booking, long>
{
    Task<GetBookingDto?> GetDtoByConfirmationCodeAsync(string confirmationCode, CancellationToken cancellationToken);
}
