using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class AirlineProfile : Profile
{
    public AirlineProfile()
    {
        CreateMap<Airline, AirlineDb>().ReverseMap();
    }
}
