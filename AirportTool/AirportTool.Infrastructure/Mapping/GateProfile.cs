using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class GateProfile : Profile
{
    public GateProfile()
    {
        CreateMap<Gate, GateDb>().ReverseMap();
    }
}
