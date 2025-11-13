using Cafe.Domain;

namespace Cafe.Infrastructure;

public class MilkDecoratorRegistration : IAddOnDecoratorRegistration
{
    private readonly string _name;
    private readonly decimal _baseCost;

    public MilkDecoratorRegistration(string name, decimal baseCost)
    {
        _name = name;
        _baseCost = baseCost;
    }

    public string Key => _name.ToLower();

    public Result<IBeverage> Create(IBeverage inner, params object[] args)
    {
        var result = new Result<IBeverage>();
        var milkDecorator = new MilkDecorator(inner, _name, _baseCost);
        var validationResult = milkDecorator.Validate();

        if (validationResult.IsFailure)
        {
            result.AddErrors(validationResult.Errors);
            return result;
        }

        result.Value = milkDecorator;
        return result;
    }
}