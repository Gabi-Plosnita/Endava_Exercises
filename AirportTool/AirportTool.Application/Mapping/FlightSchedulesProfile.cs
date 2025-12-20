using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Application;

public class FlightSchedulesProfile : Profile
{
    public FlightSchedulesProfile()
    {
        CreateMap<UpsertFlightScheduleDto, FlightSchedule>();
    }
}
