namespace Cafe.Domain;

public class ExtraShotDecorator : BaseBeverage
{
    private readonly IBeverage _inner;

    public ExtraShotDecorator(IBeverage inner, string name, decimal baseCost) : base(name, baseCost)
    {
        _inner = inner;
    }

    public override decimal Cost() => _inner.Cost() + _baseCost;

    public override string Describe() => $"{_inner.Describe()}, {Name}";
}
