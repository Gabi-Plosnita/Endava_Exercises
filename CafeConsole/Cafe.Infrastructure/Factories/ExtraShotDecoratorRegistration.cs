using Cafe.Domain;

namespace Cafe.Infrastructure;

public sealed class ExtraShotDecoratorRegistration : IDecoratorRegistration
{
    private readonly string _name;
    private readonly decimal _baseCost;

    public ExtraShotDecoratorRegistration(string name, decimal baseCost)
    {
        _name = name;
        _baseCost = baseCost;
    }

    public string Key => "extrashot";

    public Result<IBeverage> Create(IBeverage inner, params object[] args)
    {
        var result = new Result<IBeverage>();
        var extraShotDecorator = new ExtraShotDecorator(inner, _name, _baseCost);
        var validationResult = extraShotDecorator.Validate();

        if (validationResult.IsFailure)
        {
            result.AddErrors(validationResult.Errors);
            return result;
        }

        result.Value = extraShotDecorator;
        return result;
    }
}
