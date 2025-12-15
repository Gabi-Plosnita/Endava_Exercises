using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public class AirportRepository : EfRepositoryBase<Airport, AirportDb, int>, IAirportRepository
{
    public AirportRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }

    public async Task<Airport?> GetByIataCodeAsync(string iataCode, CancellationToken cancellationToken = default)
    {
        var airportDb = await _context.Airports
                                      .AsNoTracking()
                                      .SingleOrDefaultAsync(a => a.Iatacode == iataCode, cancellationToken);

        var airport = _mapper.Map<Airport>(airportDb);
        return airport;
    }
}
