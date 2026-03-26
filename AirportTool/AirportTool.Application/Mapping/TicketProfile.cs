using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Application;

public class TicketProfile : Profile
{
    public TicketProfile()
    {
        CreateMap<CreateTicketDto, Ticket>();
        CreateMap<UpdateTicketDto, Ticket>();
    }
}
