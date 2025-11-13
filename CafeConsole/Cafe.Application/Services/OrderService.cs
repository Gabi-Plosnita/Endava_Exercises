using Cafe.Domain;

namespace Cafe.Application;

public class OrderService : IOrderService
{
    private readonly IPricingStrategyProvider _pricingStrategyProvider;
    private readonly IBeverageFactory _beverageFactory;
    private readonly IAddOnDecoratorFactory _addOnDecoratorFactory;
    private readonly IOrderEventPublisher _orderEventPublisher;

    public OrderService(
        IPricingStrategyProvider pricingStrategyProvider,
        IBeverageFactory beverageFactory,
        IAddOnDecoratorFactory addOnDecoratorFactory,
        IOrderEventPublisher orderEventPublisher)
    {
        _pricingStrategyProvider = pricingStrategyProvider;
        _beverageFactory = beverageFactory;
        _addOnDecoratorFactory = addOnDecoratorFactory;
        _orderEventPublisher = orderEventPublisher;
    }

    public Result PlaceOrder(OrderRequest orderRequest)
    {
        var result = new Result();

        var pricingStrategyResult = _pricingStrategyProvider.Get(orderRequest.PricingStrategyType);
        result.AddErrors(pricingStrategyResult.Errors);

        var beverageResult = _beverageFactory.Create(orderRequest.BeverageType);
        result.AddErrors(beverageResult.Errors);

        if (result.IsFailure)
        {
            return result;
        }

        var finalBeverage = beverageResult.Value!;
        var pricingStrategy = pricingStrategyResult.Value!;
        foreach (var addOnSelection in orderRequest.AddOns)
        {
            var decoratedBeverageResult = _addOnDecoratorFactory.Create(finalBeverage, addOnSelection);
            result.AddErrors(decoratedBeverageResult.Errors);
            if (result.IsFailure)
            {
                return result;
            }
            finalBeverage = decoratedBeverageResult.Value!;
        }

        var orderPlaced = new OrderPlaced(finalBeverage, pricingStrategy);
        _orderEventPublisher.PublishOrderPlaced(orderPlaced);

        return result;
    }
}
