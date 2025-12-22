using AirportTool.Application;
using AirportTool.Infrastructure;
using AirportTool.WebApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register Api Helpers //
builder.Services.AddSingleton<IResultSeverityResolver, ResultSeverityResolver>();

// Register Application Services //
builder.Services.AddScoped<IAircraftService, AircraftService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IFlightSchedulesService, FlightSchedulesService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IGateService, GateService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Application Repositories //
builder.Services.AddScoped<IAircraftRepository, AircraftRepository>();
builder.Services.AddScoped<IAirlineRepository, AirlineRepository>();
builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IFlightScheduleRepository, FlightScheduleRepository>();
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IGateRepository, GateRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();

// Register Application Helpers //
builder.Services.AddSingleton<IDtoValidator, AnnotationDtoValidator>();
builder.Services.AddSingleton<IUniqueCodeGenerator, AlphanumericUppercaseCodeGenerator>();

// Register Application Mappings //
builder.Services.AddAutoMapper(typeof(AirportTool.Application.AssemblyReference));

// Register Infrastructure DbContext
builder.Services.AddDbContext<AirportDbContext>(options =>options.UseSqlServer(builder.Configuration.GetConnectionString("AirportDb")));

// Register Infrastructure Mapping Profiles //
builder.Services.AddAutoMapper(typeof(AirportTool.Infrastructure.AssemblyReference));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
