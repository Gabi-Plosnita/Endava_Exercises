using Cafe.Domain;

namespace Cafe.Infrastructure;

public class TeaRegistration : IBeverageRegistration
{
    private readonly string _name;
    private readonly decimal _baseCost;

    public TeaRegistration(string name, decimal baseCost)
    {
        _name = name;
        _baseCost = baseCost;
    }

    public string Key => _name.ToLower();


    public Result<IBeverage> Create()
    {
        var result = new Result<IBeverage>();
        var tea = new Tea(_name, _baseCost);
        var validationResult = tea.Validate();

        if (validationResult.IsFailure)
        {
            result.AddErrors(validationResult.Errors);
            return result;
        }

        result.Value = tea;
        return result;
    }
}
