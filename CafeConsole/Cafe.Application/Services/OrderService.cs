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

        var pricingStrategyResult = _pricingStrategyProvider.Get(orderRequest.PricingStrategyKey);
        result.AddErrors(pricingStrategyResult.Errors);

        var beverageResult = _beverageFactory.Create(orderRequest.BeverageType);
        result.AddErrors(beverageResult.Errors);

        if (result.IsFailure)
        {
            return result;
        }

        var baseBeverage = beverageResult.Value!;
        var pricingStrategy = pricingStrategyResult.Value!;
        
        var decoratedBeverageResult = ApplyAddOns(baseBeverage, orderRequest.AddOns);
        result.AddErrors(decoratedBeverageResult.Errors);
        if (result.IsFailure)
        {
            return result;
        }

        var finalBeverage = decoratedBeverageResult.Value!;
        var orderPlaced = new OrderPlaced(finalBeverage, pricingStrategy);
        _orderEventPublisher.PublishOrderPlaced(orderPlaced);

        return result;
    }

    private Result<IBeverage> ApplyAddOns(IBeverage baseBeverage, IEnumerable<AddOnSelection> addOns)
    {
        var result = new Result<IBeverage>();
        IBeverage currentBeverage = baseBeverage;
        foreach (var addOn in addOns)
        {
            var addOnResult = _addOnDecoratorFactory.Create(addOn.AddOnKey, currentBeverage, addOn.Args);
            result.AddErrors(addOnResult.Errors);
            if (result.IsFailure)
            {
                return result;
            }
            currentBeverage = addOnResult.Value!;
        }
        result.Value = currentBeverage;
        return result;
    }
}
