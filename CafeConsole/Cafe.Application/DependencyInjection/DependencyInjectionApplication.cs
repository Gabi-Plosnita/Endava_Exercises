using Cafe.Domain;
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
}
