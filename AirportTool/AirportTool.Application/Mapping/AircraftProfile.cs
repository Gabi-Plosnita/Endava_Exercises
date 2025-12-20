using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Application;

public class AircraftProfile : Profile
{
    public AircraftProfile()
    {
        CreateMap<CreateAircraftDto, Aircraft>();
        CreateMap<UpdateAircraftDto, Aircraft>();
    }
}
