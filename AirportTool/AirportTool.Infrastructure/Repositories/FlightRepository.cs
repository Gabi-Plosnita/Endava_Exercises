using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class FlightRepository : EfRepositoryBase<Flight, FlightDb, int>, IFlightRepository
{
    public FlightRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }
}
