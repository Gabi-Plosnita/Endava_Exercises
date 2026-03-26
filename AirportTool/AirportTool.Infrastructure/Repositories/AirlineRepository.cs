using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public class AirlineRepository : EfRepositoryBase<Airline, AirlineDb, int>, IAirlineRepository
{
    public AirlineRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }

    public async Task<Airline?> GetByIataCodeAsync(string iataCode, CancellationToken cancellationToken)
    {
        var airlineDb = await _context.Airlines
                                      .AsNoTracking()
                                      .SingleOrDefaultAsync(a => a.Iatacode == iataCode, cancellationToken);

        return _mapper.Map<Airline>(airlineDb);
    }
}
