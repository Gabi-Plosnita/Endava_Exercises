using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Application;

public class FlightProfile : Profile
{
    public FlightProfile()
    {
        CreateMap<CreateFlightDto, Flight>();
        CreateMap<UpdateFlightDto, Flight>();
    }
}
