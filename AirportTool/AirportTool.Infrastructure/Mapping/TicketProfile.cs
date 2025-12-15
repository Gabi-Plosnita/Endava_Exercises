using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class TicketProfile : Profile
{
    public TicketProfile()
    {
        CreateMap<Ticket, TicketDb>().ReverseMap();
    }
}
