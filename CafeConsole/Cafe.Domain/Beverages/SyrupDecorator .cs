namespace Cafe.Domain;

public class SyrupDecorator : IBeverage
{
    private IBeverage _inner;
    private string _name;
    private decimal _baseCost;
    private string _flavor;

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
