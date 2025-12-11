using AirportTool.Domain;

namespace AirportTool.Application;

public interface IFlightRepository : IRepository<Flight, int>
{
    Task<IReadOnlyCollection<FlightSearchDto>> GetFilteredFlightsAsync(
        FlightFilterDto filter, 
        CancellationToken cancellationToken = default);
}
