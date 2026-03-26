using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Application;

public class GateProfile : Profile
{
    public GateProfile()
    {
        CreateMap<CreateGateDto, Gate>();
        CreateMap<UpdateGateDto, Gate>();
    }
}
