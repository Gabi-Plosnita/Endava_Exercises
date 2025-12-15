using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class FlightProfile : Profile
{
    public FlightProfile()
    {
        CreateMap<Flight, FlightDb>().ReverseMap();
    }
}
