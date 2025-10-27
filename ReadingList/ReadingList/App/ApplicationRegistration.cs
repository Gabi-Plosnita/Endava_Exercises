using Microsoft.Extensions.DependencyInjection;

namespace ReadingList.App;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CommandDispatcher>();
        services.AddScoped<CommandApp>();

        services.AddScoped<ICommand, ImportCommand>();
        services.AddScoped<ICommand, ListAllCommand>();
        return services;
    }
}
