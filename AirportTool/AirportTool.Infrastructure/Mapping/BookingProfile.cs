using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Infrastructure;

public class BookingProfile : Profile
{
    public BookingProfile()
    {
        CreateMap<Booking, BookingDb>().ReverseMap();
    }
}
