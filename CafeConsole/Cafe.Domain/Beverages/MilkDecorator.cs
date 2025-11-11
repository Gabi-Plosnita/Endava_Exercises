namespace Cafe.Domain;

public class MilkDecorator : IBeverage
{
    private IBeverage _inner;
    private string _name;
    private decimal _baseCost;

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
