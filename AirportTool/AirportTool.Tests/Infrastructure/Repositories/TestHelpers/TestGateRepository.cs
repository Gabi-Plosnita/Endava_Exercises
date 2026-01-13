using AirportTool.Domain;
using AirportTool.Infrastructure;
using AutoMapper;

namespace AirportTool.Tests.Infrastructure.Repositories;

internal sealed class TestGateRepository : EfRepositoryBase<Gate, GateDb, int>
{
    public TestGateRepository(AirportDbContext context, IMapper mapper)
        : base(context, mapper)
    {
    }
}
