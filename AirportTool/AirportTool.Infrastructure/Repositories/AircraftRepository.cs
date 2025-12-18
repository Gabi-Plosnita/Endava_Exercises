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

    public async Task<PagedResult<GetAircraftDto>> GetDtoByFilterAsync(
        AircraftFilterDto filterDto, CancellationToken cancellationToken)
    {
        var skip = filterDto.PageIndex * filterDto.PageSize;

        var query = _context.Aircraft.AsNoTracking();

        if (!string.IsNullOrEmpty(filterDto.AirlineIataCode))
        {
            query = query.Where(a => a.OwnedByAirline != null && a.OwnedByAirline.Iatacode == filterDto.AirlineIataCode);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query.OrderBy(a => a.AircraftId)
                               .Skip(skip)
                               .Take(filterDto.PageSize)
                               .Select(a => new GetAircraftDto
                               {
                                   AircraftId = a.AircraftId,
                                   TailNumber = a.TailNumber,
                                   Model = a.Model,
                                   SeatCapacity = a.SeatCapacity,
                                   OwnedByAirlineIataCode = a.OwnedByAirline != null ? a.OwnedByAirline.Iatacode : null,
                                   OwnedByAirlineName = a.OwnedByAirline != null ? a.OwnedByAirline.Name : null
                               })
                               .ToListAsync(cancellationToken);

        return new PagedResult<GetAircraftDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = filterDto.PageIndex,
            PageSize = filterDto.PageSize
        };
    }

    public async Task AddAndSaveAsync(Aircraft aircraft, CancellationToken cancellationToken)
    {
        var aircraftDb = _mapper.Map<AircraftDb>(aircraft);
        await _context.Aircraft.AddAsync(aircraftDb, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _mapper.Map(aircraftDb, aircraft);
    }
}
