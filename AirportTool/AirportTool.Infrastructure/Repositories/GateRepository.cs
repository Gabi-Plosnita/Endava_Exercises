using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AirportTool.Infrastructure;

public class GateRepository : EfRepositoryBase<Gate, GateDb, int>, IGateRepository
{
    public GateRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }

    public Task<GetGateDto?> GetDtoByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _context.Gates
                       .AsNoTracking()
                       .Where(g => g.GateId == id)
                       .Select(g => new GetGateDto
                       {
                           GateId = g.GateId,
                           AirportIataCode = g.Airport.Iatacode,
                           AirportName = g.Airport.Name,
                           Code = g.Code
                       })
                       .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task AddAndSaveAsync(Gate gate, CancellationToken cancellationToken)
    {
        var gateDb = _mapper.Map<Gate>(gate);
        await _context.AddAsync(gateDb, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _mapper.Map(gateDb, gate);
    }
}
