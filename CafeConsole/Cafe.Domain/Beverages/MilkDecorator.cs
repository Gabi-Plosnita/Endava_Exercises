namespace Cafe.Domain;

public class MilkDecorator : BaseBeverage
{
    private readonly IBeverage _inner;

    public MilkDecorator(IBeverage inner, string name, decimal cost) : base(name, cost)
    {
        _inner = inner;
    }

    public override decimal Cost() => _inner.Cost() + _baseCost;

    public override string Describe() => $"{_inner.Describe()}, {Name}";
}
