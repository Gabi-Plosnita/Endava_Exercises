namespace Cafe.Domain;

public class SyrupDecorator : BaseBeverage
{
    private readonly IBeverage _inner;
    private readonly string _flavor;

    public SyrupDecorator(IBeverage inner, string name, decimal cost, string flavor) : base(name, cost)
    {
        _inner = inner;
        _flavor = flavor;
    }

    public override decimal Cost() => _inner.Cost() + _baseCost;

    public override string Describe() => $"{_inner.Describe()}, {_flavor} {Name}";

    public override Result Validate()
    {
        var result = base.Validate();
        if (string.IsNullOrWhiteSpace(_flavor))
        {
            result.AddError("Syrup flavor cannot be null or empty.");
        }
        return result;
    }   
}
