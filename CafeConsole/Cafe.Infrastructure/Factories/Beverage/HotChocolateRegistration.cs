using Cafe.Domain;

namespace Cafe.Infrastructure;

public class HotChocolateRegistration : IBeverageRegistration
{
    private readonly string _name;
    private readonly decimal _baseCost;

    public HotChocolateRegistration(string name, decimal baseCost)
    {
        _name = name;
        _baseCost = baseCost;
    }

    public string Key => _name.ToLower();

    public Result<IBeverage> Create()
    {
        var result = new Result<IBeverage>();
        var hotChocolate = new HotChocolate(_name, _baseCost);
        var validationResult = hotChocolate.Validate();

        if (validationResult.IsFailure)
        {
            result.AddErrors(validationResult.Errors);
            return result;
        }

        result.Value = hotChocolate;
        return result;
    }
}
