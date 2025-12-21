namespace AirportTool.Application;

public interface IBookingService
{
    public Task<GetBookingDto?> GetBookingByCodeAsync(string code, CancellationToken cancellationToken);

    public Task<Result<GetBookingDto?>> CreateBookingAsync(CreateBookingDto createBookingDto, CancellationToken cancellationToken);

    public Task<Result> CancelBookingAsync(string code, CancellationToken cancellationToken);
}
