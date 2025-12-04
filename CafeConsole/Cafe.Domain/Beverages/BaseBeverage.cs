namespace Cafe.Domain;

public abstract class BaseBeverage : IBeverage
{
    protected readonly string _name;
    protected readonly decimal _baseCost;

    protected BaseBeverage(string name, decimal baseCost)
    {
        _name = name;
        _baseCost = baseCost;
    }

    public virtual string Name => _name;

    public virtual decimal Cost() => _baseCost;

    public virtual string Describe() => _name;

    public virtual Result Validate()
    {
        var result = new Result();
        result.AddErrors(ValidateName().Errors);
        result.AddErrors(ValidateCost().Errors);
        return result;
    }

    protected virtual Result ValidateName()
    {
        var result = new Result();
        if (string.IsNullOrWhiteSpace(_name))
        {
            result.AddError(DomainConstants.BeverageNameCannotBeEmpty);
        }
        return result;
    }

    protected virtual Result ValidateCost()
    {
        var result = new Result();
        if (_baseCost <= 0)
        {
            result.AddError(DomainConstants.BeverageCostMustBeGreaterThanZero);
        }
        return result;
    }
}
