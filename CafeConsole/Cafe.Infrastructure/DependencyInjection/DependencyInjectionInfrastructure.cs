using Cafe.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Cafe.Infrastructure;

public static class DependencyInjectionInfrastructure
{
    public static IServiceCollection RegisterInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOrderEventSubscriber, InMemoryOrderAnalytics>();
        services.AddSingleton<IOrderEventSubscriber>(sp => new ConsoleOrderLogger("USD"));

        // All theese values like Name and Price can be moved in a json configuration file. I left them here for simplicity.
        services.AddSingleton<IDecoratorRegistration>(sp => new MilkDecoratorRegistration("Milk", 0.40m));
        services.AddSingleton<IDecoratorRegistration>(sp => new SyrupDecoratorRegistration("Syrup", 0.50m));
        services.AddSingleton<IDecoratorRegistration>(sp => new ExtraShotDecoratorRegistration("Extra_shot", 0.80m));
        services.AddSingleton<IAddOnDecoratorFactory, AddOnDecoratorFactory>();

        return services;
    }
}
