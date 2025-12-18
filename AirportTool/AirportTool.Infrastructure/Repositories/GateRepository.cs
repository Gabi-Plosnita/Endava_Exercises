using AirportTool.Application;
using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class GateRepository : EfRepositoryBase<Gate, GateDb, int>, IGateRepository
{
    public GateRepository(AirportDbContext context, IMapper mapper) : base(context, mapper)
    {
    }
}
