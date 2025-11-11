namespace Cafe.Domain;

public class MilkDecorator : IBeverage
{
    private readonly IBeverage _inner;
    private readonly string _name;
    private readonly decimal _baseCost;

    public MilkDecorator(IBeverage inner, string name, decimal cost)
    {
        _inner = inner;
        _name = name;
        _baseCost = cost;
    }

    public string Name => _name;

    public decimal Cost() => _inner.Cost() + _baseCost;

    public string Describe() => $"{_inner.Describe()}, {Name}";
}
