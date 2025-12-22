using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class AircraftProfile : Profile
{
    public AircraftProfile()
    {
        CreateMap<Aircraft, AircraftDb>().ReverseMap();
    }
}
