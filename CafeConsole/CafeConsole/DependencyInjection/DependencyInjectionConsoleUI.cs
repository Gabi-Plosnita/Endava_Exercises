using Microsoft.Extensions.DependencyInjection;

namespace Cafe.ConsoleUI;

public static class DependencyInjectionConsoleUI
{
    public static IServiceCollection RegisterConsoleUI(this IServiceCollection services)
    {
        services.AddSingleton<Menu>();
        return services;
    }
}
