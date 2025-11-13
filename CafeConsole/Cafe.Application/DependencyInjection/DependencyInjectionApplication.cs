using Cafe.Domain;
using Cafe.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cafe.Application;

public static class DependencyInjectionApplication
{
    public static IServiceCollection RegisterApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPricingStrategy, RegularPricing>();
        services.AddSingleton<IPricingStrategy, HappyHourPricing>();
        services.AddSingleton<IPricingStrategyProvider, InMemoryPricingStrategyProvider>();
        services.AddSingleton<IOrderEventPublisher, SimpleOrderEventPublisher>();

        services.AddSingleton<IOrderService, OrderService>();
        return services;
    }

    public static IServiceCollection RegisterInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOrderEventSubscriber, InMemoryOrderAnalytics>();
        services.AddSingleton<IOrderEventSubscriber>(sp => new ConsoleOrderLogger("USD"));

        // All theese values like Name and Price can be moved in a json configuration file. I left them here for simplicity.
        services.AddSingleton<IBeverageRegistration>(sp => new EspressoRegistration("Espresso", 2.50m));
        services.AddSingleton<IBeverageRegistration>(sp => new TeaRegistration("Tea", 2.00m));
        services.AddSingleton<IBeverageRegistration>(sp => new HotChocolateRegistration("Hot Chocolate", 3.00m));

        services.AddSingleton<IAddOnDecoratorRegistration>(sp => new MilkDecoratorRegistration("Milk", 0.40m));
        services.AddSingleton<IAddOnDecoratorRegistration>(sp => new SyrupDecoratorRegistration("Syrup", 0.50m));
        services.AddSingleton<IAddOnDecoratorRegistration>(sp => new ExtraShotDecoratorRegistration("Extra Shot", 0.80m));

        services.AddSingleton<IBeverageFactory, BeverageFactory>();
        services.AddSingleton<IAddOnDecoratorFactory, AddOnDecoratorFactory>();
        return services;
    }
}
