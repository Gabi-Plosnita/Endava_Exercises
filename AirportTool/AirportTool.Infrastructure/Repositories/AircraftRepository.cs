using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public class AircraftRepository : EfRepositoryBase<Aircraft, AircraftDb, int>, IAircraftRepository
{
    public AircraftRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }

    public async Task<Aircraft?> GetByTailNumberAsync(string tailNumber, CancellationToken cancellationToken)
    {
        var aircraftDb = await _context.Aircraft
                                       .AsNoTracking()
                                       .SingleOrDefaultAsync(a => a.TailNumber == tailNumber, cancellationToken);

        return _mapper.Map<Aircraft>(aircraftDb);
    }

    public Task<GetAircraftDto?> GetDtoByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _context.Aircraft
                       .AsNoTracking()
                       .Where(a => a.AircraftId == id)
                       .Select(a => new GetAircraftDto
                       {
                           AircraftId = a.AircraftId,
                           TailNumber = a.TailNumber,
                           Model = a.Model,
                           SeatCapacity = a.SeatCapacity,
                           OwnedByAirlineIataCode = a.OwnedByAirline != null ? a.OwnedByAirline.Iatacode : null,
                           OwnedByAirlineName = a.OwnedByAirline != null ? a.OwnedByAirline.Name : null
                       })
                       .SingleOrDefaultAsync(cancellationToken);
    }
}
