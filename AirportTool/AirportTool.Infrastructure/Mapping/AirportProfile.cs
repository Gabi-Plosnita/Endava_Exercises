using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class AirportProfile : Profile   
{
    public AirportProfile()
    {
        CreateMap<Airport, AirportDb>().ReverseMap();
    }
}
