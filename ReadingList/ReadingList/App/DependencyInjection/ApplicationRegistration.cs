using Microsoft.Extensions.DependencyInjection;

namespace ReadingList.App;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();

        services.AddScoped<CommandDispatcher>();
        services.AddScoped<CommandApp>();

        services.AddScoped<ICommand, ImportCommand>();
        services.AddScoped<ICommand, ListAllCommand>();
        services.AddScoped<ICommand, FilterFinishedCommand>();
        services.AddScoped<ICommand, TopRatedCommand>();
        services.AddScoped<ICommand, ByAuthorCommand>();
        services.AddScoped<ICommand, StatsCommand>();
        services.AddScoped<ICommand, MarkFinishedCommand>();
        services.AddScoped<ICommand, RateCommand>();
        services.AddScoped<ICommand, ExportCommand>();
        return services;
    }
}
