using Cafe.Domain;

namespace Cafe.Infrastructure;

public class EspressoRegistration : IBeverageRegistration
{
    private readonly string _name;
    private readonly decimal _baseCost;

    public EspressoRegistration(string name, decimal baseCost)
    {
        _name = name;
        _baseCost = baseCost;
    }

    public string Key => _name;

    public Result<IBeverage> Create()
    {
        var result = new Result<IBeverage>();
        var espresso = new Espresso(_name, _baseCost);
        var validationResult = espresso.Validate();

        if (validationResult.IsFailure)
        {
            result.AddErrors(validationResult.Errors);
            return result;
        }

        result.Value = espresso;
        return result;
    }
}
