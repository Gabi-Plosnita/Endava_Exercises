namespace Cafe.Domain;

public class SyrupDecorator : IBeverage
{
    private readonly IBeverage _inner;
    private readonly string _name;
    private readonly decimal _baseCost;
    private readonly string _flavor;

    public SyrupDecorator(IBeverage inner, string name, decimal cost, string flavor)
    {
        _inner = inner;
        _name = name;
        _baseCost = cost;
        _flavor = flavor;
    }

    public string Name => $"{_flavor} {_name}";

    public decimal Cost() => _inner.Cost() + _baseCost;

    public string Describe() => $"{_inner.Describe()}, {Name}";
}
