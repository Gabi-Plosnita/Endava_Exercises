using Cafe.Domain;

namespace Cafe.Infrastructure;

public class SyrupDecoratorRegistration : IDecoratorRegistration
{
    private readonly string _name;
    private readonly decimal _baseCost;

    public SyrupDecoratorRegistration(string name, decimal baseCost)
    {
        _name = name;
        _baseCost = baseCost;
    }

    public string Key => _name.ToLower();

    public Result<IBeverage> Create(IBeverage inner, params object[] args)
    {
        var result = new Result<IBeverage>();

        if (args.Length == 0 || args[0] is not string flavor || string.IsNullOrWhiteSpace(flavor))
        {
            result.AddError("Flavor (string) is required for 'syrup' at args[0].");
            return result;
        }

        var syrupDecorator = new SyrupDecorator(inner, _name, _baseCost, flavor);
        var validationResult = syrupDecorator.Validate();

        if (validationResult.IsFailure)
        {
            result.AddErrors(validationResult.Errors);
            return result;
        }

        result.Value = syrupDecorator;
        return result;
    }
}
