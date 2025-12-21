using AirportTool.Domain;
using AutoMapper;

namespace AirportTool.Application;

public class BookingProfile : Profile
{
    public BookingProfile()
    {
        CreateMap<Booking,GetBookingDto>();
        CreateMap<CreateBookingDto, Booking>();
    }
}
