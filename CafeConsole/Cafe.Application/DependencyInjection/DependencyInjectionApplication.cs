using Cafe.Domain;
using Cafe.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cafe.Application;

public static class DependencyInjectionApplication
{
    public static IServiceCollection RegisterApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPricingStrategy, RegularPricingStrategy>();
        services.AddSingleton<IPricingStrategy>(sp => new HappyHourPricingStrategy(0.2m));
        services.AddSingleton<IPricingStrategyProvider, InMemoryPricingStrategyProvider>();
        services.AddSingleton<IOrderEventPublisher, SimpleOrderEventPublisher>();

        services.AddSingleton<IOrderService, OrderService>();
        return services;
    }

    public static IServiceCollection RegisterInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOrderEventSubscriber, InMemoryOrderAnalytics>();
        services.AddSingleton<IOrderEventSubscriber>(sp => new ConsoleOrderLogger(DomainConstants.UsdCurrency));

        services.AddSingleton<IBeverageRegistration>(sp => 
            new EspressoRegistration(DomainConstants.Espresso, DomainConstants.EspressoPrice));

        services.AddSingleton<IBeverageRegistration>(sp => 
            new TeaRegistration(DomainConstants.Tea, DomainConstants.TeaPrice));

        services.AddSingleton<IBeverageRegistration>(sp => 
            new HotChocolateRegistration(DomainConstants.HotChocolate, DomainConstants.HotChocolatePrice));

        services.AddSingleton<IAddOnDecoratorRegistration>(sp => 
            new MilkDecoratorRegistration(DomainConstants.Milk, DomainConstants.MilkPrice));
        
        services.AddSingleton<IAddOnDecoratorRegistration>(sp => 
            new SyrupDecoratorRegistration(DomainConstants.Syrup, DomainConstants.SyrupPrice));
        
        services.AddSingleton<IAddOnDecoratorRegistration>(sp => 
        new ExtraShotDecoratorRegistration(DomainConstants.ExtraShot, DomainConstants.ExtraShotPrice));

        services.AddSingleton<IBeverageFactory, BeverageFactory>();
        services.AddSingleton<IAddOnDecoratorFactory, AddOnDecoratorFactory>();
        return services;
    }
}
