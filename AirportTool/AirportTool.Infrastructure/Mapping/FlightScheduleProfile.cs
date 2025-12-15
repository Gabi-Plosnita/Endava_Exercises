using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class FlightScheduleProfile : Profile
{
    public FlightScheduleProfile()
    {
        CreateMap<FlightSchedule, FlightScheduleDb>().ReverseMap();
    }
}
